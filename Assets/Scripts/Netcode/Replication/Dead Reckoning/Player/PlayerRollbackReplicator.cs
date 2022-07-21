using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(CharacterKinematics))]
public sealed class PlayerRollbackReplicator : NetworkBehaviour
{
    private ProjectionShape proj;
    private CharacterKinematics kinematics;

#if UNITY_EDITOR
    //Exists only so Unity shows the enable/disable checkbox
    private void Update() { }
#endif

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        proj = ProjectionShape.Build(gameObject);
        kinematics = GetComponent<CharacterKinematics>();
        Debug.Assert(proj != null);
        Debug.Assert(kinematics != null);

        if (IsServer)
        {
            //Set initial value
            speculativeFutures.Clear();
            speculativeFutures.Enqueue(kinematics.frame);
        }

        if (IsClient)
        {
            //Set initial value
            unvalidatedHistory.Clear();
            unvalidatedHistory.Enqueue(kinematics.frame);

            //Hook
            kinematics.FinalizeMove += SubmitFrameToServer;
        }
    }

    //Speculative frames are erased when the appropriate client frame arrives.
    //Unvalidated history frames are erased once confirmed by the server, or are adjusted if rejected.
    [SerializeField] private RecyclingLinkedQueue<PlayerPhysicsFrame> speculativeFutures = new RecyclingLinkedQueue<PlayerPhysicsFrame>();
    [SerializeField] private RecyclingLinkedQueue<PlayerPhysicsFrame> unvalidatedHistory = new RecyclingLinkedQueue<PlayerPhysicsFrame>();

    private static void TrimBefore(RecyclingLinkedQueue<PlayerPhysicsFrame> list, float cutoffTime)
    {
        while (list.Count>1 && list.Peek().time < cutoffTime) list.DropHead();
    }

    [Flags]
    private enum RejectionFlags
    {
        Position = (1 << 0),
        Velocity = (1 << 1),
        Look     = (1 << 2),

        All = Position | Velocity | Look
    }

    private void RecalcAfter(RecyclingLinkedQueue<PlayerPhysicsFrame> frames, float sinceTime)
    {
        try
        {
            Recalc(frames, frames.FindNode(i => sinceTime < i.time));
        }
        catch (IndexOutOfRangeException) { } //If none match, fail silently
    }

    private void Recalc(RecyclingLinkedQueue<PlayerPhysicsFrame> frames, RecyclingNode<PlayerPhysicsFrame> lastValid)
    {
        if (lastValid == null) return;

        for (RecyclingNode<PlayerPhysicsFrame> i = lastValid; i.next != null; i = i.next)
        {
            PlayerPhysicsFrame next = i.value;
            next.inputMove = i.next.value.inputMove;
            next.inputJump = i.next.value.inputJump;
            next.look = i.next.value.look;
            next = kinematics.Step(i.value, i.next.value.time-i.value.time, CharacterKinematics.StepMode.SimulateRecalc);
            i.next.value = next;
        }
    }

    private void SubmitFrameToServer()
    {
#if UNITY_EDITOR
        //Testing-only, allows use in other scenes without network setup
        if (!IsSpawned || !isActiveAndEnabled) return;
#endif

        PlayerPhysicsFrame adjustedFrame = kinematics.frame;
        adjustedFrame.time -= NetHeartbeat.Self.SmoothedRTT/2; //Adjust for travel delay in builtin time sync

        //Push to history
        unvalidatedHistory.Enqueue(adjustedFrame);

        //Send to server
        //Send (50/4) = ~12 frames per second
        if (IsLocalPlayer && kinematics.frame.id%4==0) DONOTCALL_ReceiveClientFrame_ServerRpc(adjustedFrame);
    }

    [Header("Validation (server-side only)")]
    [SerializeField] [Min(0.001f)] private float positionForgiveness = 0.05f;
    [SerializeField] [Min(0.001f)] private float velocityForgiveness = 0.05f;

    [ServerRpc(Delivery = RpcDelivery.Unreliable, RequireOwnership = true)]
    private void DONOTCALL_ReceiveClientFrame_ServerRpc(PlayerPhysicsFrame untrustedFrame, ServerRpcParams src = default)
    {
        //Verify ownership
        if (src.Receive.SenderClientId != OwnerClientId) throw new AccessViolationException($"Player {src.Receive.SenderClientId} tried to send physics data as {OwnerClientId}!");

        Debug.Assert(speculativeFutures.Count > 0);

        if (speculativeFutures.Peek().time <= untrustedFrame.time)
        {
            RejectionFlags reject = 0;

            //Sanitize - players don't have the authority to teleport, forbid
            if (untrustedFrame.mode == PlayerPhysicsFrame.Mode.Teleport)
            {
                Debug.LogWarning("Player tried to send Teleport frame!");
                untrustedFrame.mode = PlayerPhysicsFrame.Mode.Default;
                reject |= RejectionFlags.Position | RejectionFlags.Velocity | RejectionFlags.Look;
            }

            //Sanitize - protect against input outside allowed bounds
            if (ValidationUtility.Bound(in untrustedFrame.inputMove, out untrustedFrame.inputMove, Vector2.zero, 1.02f)) reject |= RejectionFlags.Position | RejectionFlags.Velocity;

            //Sanitize - protect against cached-data attack
            untrustedFrame.RefreshLookTrig();

            //Find insert position
            RecyclingNode<PlayerPhysicsFrame> beforeInsert = speculativeFutures.FindNode(n => n.next == null || (n.value.time <= untrustedFrame.time && untrustedFrame.time <= n.next.value.time));

            //Validate - build simulation basis, transfer critical non-validated data
            PlayerPhysicsFrame prevFrame = beforeInsert.value;
            prevFrame.inputMove = untrustedFrame.inputMove;
            prevFrame.inputJump = untrustedFrame.inputJump;
            prevFrame.look      = untrustedFrame.look;
            prevFrame.id        = untrustedFrame.id-1;

            //Validate - simulate forward to create an authority frame
            PlayerPhysicsFrame authorityFrame = kinematics.Step(prevFrame, untrustedFrame.time-prevFrame.time, CharacterKinematics.StepMode.SimulateVerify);
            authorityFrame.look = untrustedFrame.look;
            Debug.Assert(authorityFrame.id == untrustedFrame.id);

            //Special case: If teleporting, always overwrite ALL values
            if (authorityFrame.mode == PlayerPhysicsFrame.Mode.Teleport) reject |= RejectionFlags.All;

            //Validate - compare untrusted value to authority value, and copy over (within bounds)
            //No point in bounding if already overwritten by a more precise source
            if (!reject.HasFlag(RejectionFlags.Position) && ValidationUtility.Bound(in untrustedFrame.position, out authorityFrame.position, authorityFrame.position, positionForgiveness)) reject |= RejectionFlags.Position;
            if (!reject.HasFlag(RejectionFlags.Velocity) && ValidationUtility.Bound(in untrustedFrame.velocity, out authorityFrame.velocity, authorityFrame.velocity, velocityForgiveness)) reject |= RejectionFlags.Velocity;

            //Finalize - record
            if (beforeInsert.value.time == authorityFrame.time) beforeInsert.value = authorityFrame; //TODO should this use stable ID instead?
            else speculativeFutures.Insert(beforeInsert, authorityFrame); //TODO should this overwrite instead?

            //Finalize - anything before is already valid by extension and therefore irrelevant
            if (reject != 0) RecalcAfter(speculativeFutures, authorityFrame.time);
            TrimBefore(speculativeFutures, authorityFrame.time);
            kinematics.frame              = speculativeFutures.Tail.value;
            kinematics.transform.position = speculativeFutures.Tail.value.position; //Force update transform so we can ignore collisions

            if (reject != 0) Debug.Log("Rejecting frame for Player #"+OwnerClientId+": "+reject);

            //Send frame to ALL players
            DONOTCALL_ReceiveAuthorityFrame_ClientRpc(authorityFrame, reject);
        }
        else
        {
            //Abort if a more recent basis frame has been validated
            Debug.LogWarning($"Player {src.Receive.SenderClientId} sent a frame ({untrustedFrame.time}) but it was already overwritten ({speculativeFutures.Peek().time})");
        }
    }

    private float rejectCooldownEndTime = -1;

    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    private void DONOTCALL_ReceiveAuthorityFrame_ClientRpc(PlayerPhysicsFrame authorityFrame, RejectionFlags reject, ClientRpcParams p = default)
    {
        //if (IsHost) return; //No need to adjust authority copy?

        //Ignore if we're in the process of responding to a rollback
        //Special case: Teleport always happens anyway
        if (authorityFrame.mode != PlayerPhysicsFrame.Mode.Teleport && authorityFrame.time < rejectCooldownEndTime) return;
        
        if (IsLocalPlayer && reject != 0) Debug.Log("Received frame-reject: "+reject);

        //If we're showing a remote player, ALL values are copied over.
        if (!IsLocalPlayer) reject = RejectionFlags.All;

        //Locate relevant frame
        RecyclingNode<PlayerPhysicsFrame> n = null;
        if (unvalidatedHistory.Tail.value.time < authorityFrame.time) n = unvalidatedHistory.Tail;
        else try
        {
            n = unvalidatedHistory.FindNode(i => (IsOwner && i.value.id == authorityFrame.id)
                                              || i.next == null
                                              || authorityFrame.time <= i.next.value.time);
        }
        catch (IndexOutOfRangeException)
        {
            Debug.LogWarning("Duplicate server response for frame at " + authorityFrame.time, this);
        }

        //Apply changes
        if (reject.HasFlag(RejectionFlags.Position)) n.value.position = authorityFrame.position;
        if (reject.HasFlag(RejectionFlags.Velocity)) n.value.velocity = authorityFrame.velocity;
        
        //If showing a remote player, copy over input values so we can continue simulating reliably
        if (!IsLocalPlayer)
        {
            n.value.inputMove = authorityFrame.inputMove;
            n.value.inputJump = authorityFrame.inputJump;
        }
        
        //Keep look angle
        Vector2 newLookAngle = reject.HasFlag(RejectionFlags.Look) ? authorityFrame.look : kinematics.frame.look;

        //Recalculate if any changes
        if (n.next != null) Recalc(unvalidatedHistory, n);

        //Start cooldown if any part was rejected
        if (IsHost && reject != 0) rejectCooldownEndTime = authorityFrame.time;
        
        //This frame should now be validated from the server's standpoint, anything before is irrelevant
        TrimBefore(unvalidatedHistory, authorityFrame.time);
        unvalidatedHistory.Tail.value.look = newLookAngle;

        if (true || !IsHost)
        {
            kinematics.frame              = unvalidatedHistory.Tail.value;
            kinematics.transform.position = unvalidatedHistory.Tail.value.position; //Force update transform so we can ignore collisions
        }
    }

    [Header("DEBUG")]
    [SerializeField] private bool showFutures;
    private void OnDrawGizmos()
    {
        if (IsSpawned && IsServer && showFutures && speculativeFutures.Count > 0)
        {
            for (RecyclingNode<PlayerPhysicsFrame> i = speculativeFutures.Head; i != null; i = i.next)
            {
                //Positions
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(i.value.position, Vector3.one*0.05f);
                //if (i.next != null) Gizmos.DrawLine(i.value.position, i.next.value.position);

                //Velocities
                Gizmos.color = Color.red;
                Gizmos.DrawLine(i.value.position, i.value.position+i.value.velocity);
            }
        }

        if (IsSpawned && IsClient && !showFutures && unvalidatedHistory.Count > 0)
        {
            for (RecyclingNode<PlayerPhysicsFrame> i = unvalidatedHistory.Head; i != null; i = i.next)
            {
                //Positions
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(i.value.position, Vector3.one*0.05f);
                //if (i.next != null) Gizmos.DrawLine(i.value.position, i.next.value.position);

                //Velocities
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(i.value.position, i.value.position+i.value.velocity);
            }
        }
    }
}
