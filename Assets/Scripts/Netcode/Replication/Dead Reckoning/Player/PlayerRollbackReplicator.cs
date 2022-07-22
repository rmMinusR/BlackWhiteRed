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
    private enum OverwriteFlags
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
            RecalcAfter(frames, frames.FindNode(i => sinceTime < i.time).node);
        }
        catch (IndexOutOfRangeException) { } //If none match, fail silently
    }

    private void RecalcAfter(RecyclingLinkedQueue<PlayerPhysicsFrame> frames, RecyclingNode<PlayerPhysicsFrame> lastValid)
    {
        if (lastValid == null) return;

        for (RecyclingNode<PlayerPhysicsFrame> i = lastValid; i.next != null; i = i.next)
        {
            PlayerPhysicsFrame next = i.value;
            next.input = i.next.value.input; //Keep input
            next.look  = i.next.value.look ; //Keep look
            next = kinematics.Step(next, i.next.value.time-i.value.time, CharacterKinematics.StepMode.SimulateRecalc);
            i.next.value = next;
        }
    }

    //TUNING ONLY - TODO STRIP BEFORE COMMIT
    [SerializeField] [Range(1, 10)] private int txFreq = 4;
    [SerializeField] [Range(-1, 1)] private float txAdj = 0;
    [SerializeField] [Range(-1, 1)] private float rxAdj = 0;

    private void SubmitFrameToServer()
    {
#if UNITY_EDITOR
        //Testing-only, allows use in other scenes without network setup
        if (!IsSpawned || !isActiveAndEnabled) return;
#endif

        PlayerPhysicsFrame adjustedFrame = kinematics.frame;
        adjustedFrame.time += NetHeartbeat.Self.SmoothedRTT * txAdj; //Adjust for travel delay in builtin time sync

        //Push to history
        unvalidatedHistory.Enqueue(adjustedFrame);

        //Send to server
        //Send (50/4) = ~12 frames per second
        if (IsLocalPlayer && kinematics.frame.id%txFreq==0) DONOTCALL_ReceiveClientFrame_ServerRpc(adjustedFrame);
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
            OverwriteFlags overwrite = 0;

            overwrite |= Sanitize(untrustedFrame);

            //Find insert position
            (RecyclingNode<PlayerPhysicsFrame> lastValid, int lastValidIndex) = speculativeFutures.FindNode(n => n.next == null || (n.value.time < untrustedFrame.time && untrustedFrame.time <= n.next.value.time));

            //Do verification simulation
            PlayerPhysicsFrame authorityFrame = SimulateVerify(kinematics, lastValid.value, untrustedFrame);

            //Validate - Special case: If teleporting, always overwrite ALL values
            if (authorityFrame.mode == PlayerPhysicsFrame.Mode.Teleport) overwrite |= OverwriteFlags.All;

            //Validate - compare untrusted value to authority value, and copy over (within bounds)
            //No point in bounding if already overwritten by a more precise source
            if (!overwrite.HasFlag(OverwriteFlags.Position) && ValidationUtility.Bound(in untrustedFrame.position, out authorityFrame.position, authorityFrame.position, positionForgiveness)) overwrite |= OverwriteFlags.Position;
            if (!overwrite.HasFlag(OverwriteFlags.Velocity) && ValidationUtility.Bound(in untrustedFrame.velocity, out authorityFrame.velocity, authorityFrame.velocity, velocityForgiveness)) overwrite |= OverwriteFlags.Velocity;

            //Finalize - record
            Debug.Log($"Recording at {lastValidIndex}: {lastValid.value.time} <= {authorityFrame.time} <= {lastValid.next?.value.time}", this);
            if (lastValid.value.time == authorityFrame.time) lastValid.value = authorityFrame; //TODO should this use stable ID instead?
            else speculativeFutures.Insert(lastValid, authorityFrame); //TODO should this overwrite instead?

            //Finalize - anything before is already valid by extension and therefore irrelevant
            if (overwrite != 0) RecalcAfter(speculativeFutures, authorityFrame.time);
            TrimBefore(speculativeFutures, authorityFrame.time);
            kinematics.frame              = speculativeFutures.Tail.value;
            kinematics.transform.position = speculativeFutures.Tail.value.position; //Force update transform so we can ignore collisions

            if (overwrite != 0) Debug.Log("Rejecting frame for Player #"+OwnerClientId+": "+overwrite);

            //Send frame to ALL players
            DONOTCALL_ReceiveAuthorityFrame_ClientRpc(authorityFrame, overwrite);
        }
        else
        {
            //Abort if a more recent basis frame has been validated
            Debug.LogWarning($"Player {src.Receive.SenderClientId} sent a frame ({untrustedFrame.time}) but it was already overwritten ({speculativeFutures.Peek().time})");
        }
    }

    private static OverwriteFlags Sanitize(PlayerPhysicsFrame untrusted)
    {
        OverwriteFlags overwrite = 0;

        //Sanitize - players don't have the authority to teleport, forbid
        if (untrusted.mode == PlayerPhysicsFrame.Mode.Teleport)
        {
            Debug.LogWarning("Player tried to send Teleport frame!");
            untrusted.mode = PlayerPhysicsFrame.Mode.Default;
            overwrite |= OverwriteFlags.Position | OverwriteFlags.Velocity | OverwriteFlags.Look;
        }

        //Sanitize - protect against input outside allowed bounds
        if (ValidationUtility.Bound(in untrusted.input.move, out untrusted.input.move, Vector2.zero, 1.02f)) overwrite |= OverwriteFlags.Position | OverwriteFlags.Velocity;

        //Sanitize - protect against cached-data attack
        untrusted.RefreshLookTrig();

        return overwrite;
    }

    private static PlayerPhysicsFrame SimulateVerify(CharacterKinematics kinematics, PlayerPhysicsFrame basis, PlayerPhysicsFrame untrusted)
    {
        basis.id = untrusted.id - 1;

        //Pre-step - simulate input
        //TODO can this be done with CharacterKinematics.PreMove callback?
        basis.input = untrusted.input;
        basis.look = untrusted.look;
        basis.RefreshLookTrig();

        //Validate - simulate forward to create an authority frame
        PlayerPhysicsFrame authorityFrame = kinematics.Step(basis, untrusted.time-basis.time, CharacterKinematics.StepMode.SimulateVerify);
        Debug.Assert(authorityFrame.id   == untrusted.id);
        Debug.Assert(authorityFrame.time == untrusted.time);

        return authorityFrame;
    }

    private float rejectCooldownEndTime = -1;

    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    private void DONOTCALL_ReceiveAuthorityFrame_ClientRpc(PlayerPhysicsFrame authorityFrame, OverwriteFlags reject, ClientRpcParams p = default)
    {
        //if (IsHost) return; //Authority copy is already up to date
        //if (IsLocalPlayer) return;

        //Ignore if we're in the process of responding to a rollback
        //Special case: Teleport always happens anyway
        if (authorityFrame.mode != PlayerPhysicsFrame.Mode.Teleport && authorityFrame.time < rejectCooldownEndTime) return;

        authorityFrame.time += NetHeartbeat.Self.SmoothedRTT * rxAdj; //Adjust for travel delay in builtin time sync

        if (IsLocalPlayer && reject != 0) Debug.Log("Received frame-reject: "+reject);

        //If we're showing a remote player, ALL values are copied over.
        if (!IsLocalPlayer) reject = OverwriteFlags.All;

        //Locate relevant frame
        RecyclingNode<PlayerPhysicsFrame> n = null;
        int nInd = -1;
        if (IsLocalPlayer)
        {
            try
            { (n, nInd) = unvalidatedHistory.FindNode(i => i.value.id == authorityFrame.id); }
            catch (IndexOutOfRangeException) { Debug.LogWarning($"Frame {authorityFrame.time} missing -- overwritten?", this); }
        }

        if (n == null && unvalidatedHistory.Tail.value.time < authorityFrame.time)
        {
            unvalidatedHistory.Enqueue(default);
            n = unvalidatedHistory.Tail;
            nInd = unvalidatedHistory.Count - 1;
            Debug.LogWarning($"#{OwnerClientId} Inserting at Tail instead of overwriting, this should never happen!", this);
        }

        //Default to first time with good continuity, or if that fails, to Tail
        if (n == null) (n, nInd) = unvalidatedHistory.FindNode(i => i.next == null || (i.value.time < authorityFrame.time && authorityFrame.time <= i.next.value.time));

        if (IsLocalPlayer) Debug.Log($"Overwriting at {nInd}/{unvalidatedHistory.Count}: {n.value.id}@{n.value.time} <= {authorityFrame.id}@{authorityFrame.time} <= {n.next?.value.id.ToString()??"(null)"}@{n.next?.value.time.ToString()??"(null)"}", this);

        //Apply changes
        if (reject.HasFlag(OverwriteFlags.Position)) n.value.position = authorityFrame.position;
        if (reject.HasFlag(OverwriteFlags.Velocity)) n.value.velocity = authorityFrame.velocity;
        
        //If showing a remote player, copy over input values so we can continue simulating reliably
        //TODO If showing a local player, copy over for consistency?
        if (!IsLocalPlayer) n.value.input.move = authorityFrame.input.move;
        if (!IsLocalPlayer) n.value.input.jump = authorityFrame.input.jump;
        if (reject.HasFlag(OverwriteFlags.Look)) n.value.look = authorityFrame.look;

        //Recalculate if any changes
        if (n.next != null) RecalcAfter(unvalidatedHistory, n);

        //Start cooldown if any part was rejected
        if (IsHost && reject != 0) rejectCooldownEndTime = authorityFrame.time;
        
        //This frame should now be validated from the server's standpoint, anything before is irrelevant
        TrimBefore(unvalidatedHistory, authorityFrame.time);
        
        if (!IsHost || IsLocalPlayer)
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
