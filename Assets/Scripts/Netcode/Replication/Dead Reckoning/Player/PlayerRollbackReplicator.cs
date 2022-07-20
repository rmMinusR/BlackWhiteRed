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
        adjustedFrame.time += NetHeartbeat.Self.SmoothedRTT/2; //Adjust for time delay

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
            //Find insert position
            RecyclingNode<PlayerPhysicsFrame> beforeInsert = speculativeFutures.Head;
            while (beforeInsert != null && beforeInsert.value.time < untrustedFrame.time) beforeInsert = beforeInsert.next;
            if (beforeInsert == null) beforeInsert = speculativeFutures.Tail; //If we didn't find anything, all < untrustedFrame.time, therefore it goes at the end

            RejectionFlags reject = 0;

            //Players don't have the authority to teleport, forbid
            if (untrustedFrame.mode == PlayerPhysicsFrame.Mode.Teleport)
            {
                Debug.LogWarning("Player tried to send Teleport frame!");
                untrustedFrame.mode = PlayerPhysicsFrame.Mode.Default;
                reject |= RejectionFlags.Position | RejectionFlags.Velocity | RejectionFlags.Look;
            }

            //Protect against cached-data attack
            if (ValidationUtility.Bound(in untrustedFrame.inputMove.x, out untrustedFrame.inputMove.x, 0, 1)
             || ValidationUtility.Bound(in untrustedFrame.inputMove.y, out untrustedFrame.inputMove.y, 0, 1)) reject |= RejectionFlags.Position | RejectionFlags.Velocity;
            untrustedFrame.RefreshLookTrig();

            //Authority frame will be the basis. Transfer critical non-validated data.
            PlayerPhysicsFrame prevFrame = beforeInsert.value;
            prevFrame.inputMove = untrustedFrame.inputMove;
            prevFrame.inputJump = untrustedFrame.inputJump;
            prevFrame.look      = untrustedFrame.look;

            //Validate - simulate forward
            PlayerPhysicsFrame authorityFrame = kinematics.Step(prevFrame, untrustedFrame.time-prevFrame.time, CharacterKinematics.StepMode.SimulateVerify);
            authorityFrame.look = untrustedFrame.look;
            
            //Special case: If teleporting, it's always correct
            //Must happen after verify
            if (authorityFrame.mode == PlayerPhysicsFrame.Mode.Teleport)
            {
                reject |= RejectionFlags.Position | RejectionFlags.Velocity | RejectionFlags.Look;
                untrustedFrame.position = authorityFrame.position;
                untrustedFrame.velocity = authorityFrame.velocity;
                untrustedFrame.look     = authorityFrame.look;
            }

            //Validate - copy critical data over
            if (ValidationUtility.Bound(in untrustedFrame.position, out authorityFrame.position, authorityFrame.position, positionForgiveness)) reject |= RejectionFlags.Position;
            if (ValidationUtility.Bound(in untrustedFrame.velocity, out authorityFrame.velocity, authorityFrame.velocity, velocityForgiveness)) reject |= RejectionFlags.Velocity;

            //Record
            if (beforeInsert.value.time == authorityFrame.time) beforeInsert.value = authorityFrame; //TODO should this use stable ID instead?
            else speculativeFutures.Insert(beforeInsert, authorityFrame); //TODO should this overwrite instead?

            //'Untrusted' frame made it through validation, anything before is already valid by extension and therefore irrelevant
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

    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    private void DONOTCALL_ReceiveAuthorityFrame_ClientRpc(PlayerPhysicsFrame authorityFrame, RejectionFlags reject, ClientRpcParams p = default)
    {
        //if (IsHost) return; //No need to adjust authority copy?

        authorityFrame.time += NetHeartbeat.Self.SmoothedRTT/2; //Adjust for time delay

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
        
        //This frame should now be validated from the server's standpoint, anything before is irrelevant
        TrimBefore(unvalidatedHistory, authorityFrame.time);
        unvalidatedHistory.Tail.value.look = newLookAngle;

        if (!IsHost)
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
