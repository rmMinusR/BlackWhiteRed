using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
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
            serverFrameFutures.Clear();
            serverFrameFutures.Enqueue(kinematics.frame);
        }

        if (IsLocalPlayer)
        {
            //Set initial value
            clientFrameHistory.Clear();
            clientFrameHistory.Enqueue(kinematics.frame);

            //Hook
            kinematics.FinalizeMove += SubmitFrameToServer;
        }
    }

    //'Server frame futures' stores speculative frames, which are erased when the appropriate client frame arrives.
    //'Client frame history' stores past frames, which are erased once confirmed by the server.
    [SerializeField] private RecyclingLinkedQueue<PlayerPhysicsFrame> serverFrameFutures = new RecyclingLinkedQueue<PlayerPhysicsFrame>();
    [SerializeField] private RecyclingLinkedQueue<PlayerPhysicsFrame> clientFrameHistory = new RecyclingLinkedQueue<PlayerPhysicsFrame>();

    private static void TrimBefore(RecyclingLinkedQueue<PlayerPhysicsFrame> list, float cutoffTime)
    {
        while (list.Peek().time < cutoffTime) list.Dequeue();
    }

    [Flags]
    private enum RejectionFlags
    {
        Position = (1 << 0),
        Velocity = (1 << 1)
    }

    [Pure]
    private void RecalcAfter(RecyclingLinkedQueue<PlayerPhysicsFrame> frames, float sinceTime)
    {
        for (RecyclingLinkedQueue<PlayerPhysicsFrame>.Node i = frames.Head; i != null; i = i.next)
        {
            if (sinceTime < i.value.time) i.next.value = kinematics.Step(i.value, i.next.value.time-i.value.time, false);
        }

        //Force update transform so we can ignore collisions
        transform.position = frames.Tail.value.position;
    }

    private void SubmitFrameToServer()
    {
        //Testing-only, allows use in other scenes without network setup
        //TODO strip
        if (!IsSpawned || !isActiveAndEnabled) return;

        //Push to history
        clientFrameHistory.Enqueue(kinematics.frame);
        DONOTCALL_ReceiveClientFrame_ServerRpc(kinematics.frame);
    }

    [Header("Validation (server-side only)")]
    [SerializeField] [Min(0.001f)] private float positionForgiveness = 0.05f;
    [SerializeField] [Min(0.001f)] private float velocityForgiveness = 0.05f;

    [ServerRpc(Delivery = RpcDelivery.Unreliable, RequireOwnership = true)]
    private void DONOTCALL_ReceiveClientFrame_ServerRpc(PlayerPhysicsFrame untrustedFrame, ServerRpcParams src = default)
    {
        //Verify ownership
        if (src.Receive.SenderClientId != OwnerClientId) throw new AccessViolationException($"Player {src.Receive.SenderClientId} tried to send physics data as {OwnerClientId}!");

        if (untrustedFrame.time <= serverFrameFutures.Peek().time)
        {
            //int insertIndex = serverFrameHistory.FindLastIndex(i => i.time < untrustedFrame.time);
            //int insertIndex = 1;
            //while (insertIndex < serverFrameFutures.Count && serverFrameFutures[insertIndex].time < untrustedFrame.time) ++insertIndex;
            RecyclingLinkedQueue<PlayerPhysicsFrame>.Node beforeInsert = serverFrameFutures.Head;
            while (beforeInsert != null && beforeInsert.value.time < untrustedFrame.time) beforeInsert = beforeInsert.next;

            //Protect against cached-data attack
            if (untrustedFrame.input.sqrMagnitude > 1) untrustedFrame.input.Normalize();
            untrustedFrame.RefreshLookTrig();
            
            //Simulate forward
            PlayerPhysicsFrame prevFrame = beforeInsert.value;
            PlayerPhysicsFrame authorityFrame = kinematics.Step(prevFrame, untrustedFrame.time-prevFrame.time, false);
            if (beforeInsert != serverFrameFutures.Tail) RecalcAfter(serverFrameFutures, untrustedFrame.time);

            //Validate - copy critical features of authority frame over
            untrustedFrame.position = ValidationUtility.Bound(out bool positionInvalid, untrustedFrame.position, authorityFrame.position, positionForgiveness);
            untrustedFrame.velocity = ValidationUtility.Bound(out bool velocityInvalid, untrustedFrame.velocity, authorityFrame.velocity, velocityForgiveness);

            //Record
            serverFrameFutures.Insert(beforeInsert, untrustedFrame);
            TrimBefore(serverFrameFutures, untrustedFrame.time); //Trim - TODO use confirmation instead

            //Send response verifying or rejecting
            RejectionFlags reject = 0;
            if (positionInvalid) reject |= RejectionFlags.Position;
            if (velocityInvalid) reject |= RejectionFlags.Velocity;
            DONOTCALL_ReceiveServerResponse_ClientRpc(untrustedFrame, reject, src.ReturnToSender());
        }
        else
        {
            //Abort if a more recent basis frame has been validated
            Debug.LogWarning($"Player {src.Receive.SenderClientId} sent a frame ({untrustedFrame.time}) but it was already overwritten ({serverFrameFutures.Peek().time})");
        }
    }

    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    private void DONOTCALL_ReceiveServerResponse_ClientRpc(PlayerPhysicsFrame frame, RejectionFlags reject, ClientRpcParams p)
    {
        //Apply changes, if any
        if (reject != 0)
        {
            //Locate relevant frame
            RecyclingLinkedQueue<PlayerPhysicsFrame>.Node n = clientFrameHistory.FindNode(i => frame.time == i.time);
            if (n == null) //Ignore if duplicate (already handled)
            {
                Debug.LogWarning("Duplicate server response for frame at "+frame.time, this);
                return; 
            }

            if (reject.HasFlag(RejectionFlags.Position)) n.value.position = frame.position;
            if (reject.HasFlag(RejectionFlags.Velocity)) n.value.velocity = frame.velocity;

            //Recalculate
            RecalcAfter(clientFrameHistory, frame.time);
        }

        TrimBefore(clientFrameHistory, frame.time); //Trim - TODO use confirmation instead
    }
}
