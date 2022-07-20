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

        if (IsClient)
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
        while (list.Count>1 && list.Peek().time < cutoffTime) list.DropHead();
    }

    [Flags]
    private enum RejectionFlags
    {
        Position = (1 << 0),
        Velocity = (1 << 1)
    }

    private void RecalcAfter(RecyclingLinkedQueue<PlayerPhysicsFrame> frames, float sinceTime)
    {
        try
        {
            Recalc(frames, frames.FindNode(i => sinceTime < i.time));
        }
        catch (IndexOutOfRangeException e) { }
    }

    private void Recalc(RecyclingLinkedQueue<PlayerPhysicsFrame> frames, RecyclingNode<PlayerPhysicsFrame> startNode)
    {
        if (startNode == null) return;

        for (RecyclingNode<PlayerPhysicsFrame> i = startNode; i.next != null; i = i.next)
        {
            i.next.value = kinematics.Step(i.value, i.next.value.time-i.value.time, CharacterKinematics.StepMode.SimulateRecalc);
        }
    }

    private void SubmitFrameToServer()
    {
#if UNITY_EDITOR
        //Testing-only, allows use in other scenes without network setup
        if (!IsSpawned || !isActiveAndEnabled) return;
#endif

        //Push to history
        clientFrameHistory.Enqueue(kinematics.frame);

        if (IsLocalPlayer)
        {
            //Send to server
            PlayerPhysicsFrame adjustedFrame = kinematics.frame;
            DONOTCALL_ReceiveClientFrame_ServerRpc(adjustedFrame);
        }
    }

    [Header("Validation (server-side only)")]
    [SerializeField] [Min(0.001f)] private float positionForgiveness = 0.05f;
    [SerializeField] [Min(0.001f)] private float velocityForgiveness = 0.05f;

    [ServerRpc(Delivery = RpcDelivery.Unreliable, RequireOwnership = true)]
    private void DONOTCALL_ReceiveClientFrame_ServerRpc(PlayerPhysicsFrame untrustedFrame, ServerRpcParams src = default)
    {
        //Verify ownership
        if (src.Receive.SenderClientId != OwnerClientId) throw new AccessViolationException($"Player {src.Receive.SenderClientId} tried to send physics data as {OwnerClientId}!");

        Debug.Assert(serverFrameFutures.Count > 0);

        if (serverFrameFutures.Peek().time <= untrustedFrame.time)
        {
            //Find insert position
            RecyclingNode<PlayerPhysicsFrame> beforeInsert = serverFrameFutures.Head;
            while (beforeInsert != null && beforeInsert.value.time < untrustedFrame.time) beforeInsert = beforeInsert.next;
            if (beforeInsert == null) beforeInsert = serverFrameFutures.Tail; //If we didn't find anything, all < untrustedFrame.time, therefore it goes at the end

            RejectionFlags reject = 0;

            //Players don't have the authority to teleport, forbid
            if (untrustedFrame.mode == PlayerPhysicsFrame.Mode.Teleport)
            {
                Debug.LogWarning("Player tried to send Teleport frame!");
                untrustedFrame.mode = PlayerPhysicsFrame.Mode.Default;
                reject |= RejectionFlags.Position | RejectionFlags.Velocity;
            }

            //Protect against cached-data attack
            if (ValidationUtility.Bound(ref untrustedFrame.input.x, 0, 1)
             || ValidationUtility.Bound(ref untrustedFrame.input.y, 0, 1)) reject |= RejectionFlags.Position | RejectionFlags.Velocity;
            untrustedFrame.RefreshLookTrig();

            //Validate - simulate forward
            PlayerPhysicsFrame prevFrame = beforeInsert.value;
            PlayerPhysicsFrame authorityFrame = kinematics.Step(prevFrame, untrustedFrame.time-prevFrame.time, CharacterKinematics.StepMode.SimulateVerify);

            //Special case: If teleporting, it's always correct
            //Must happen after verify
            if (authorityFrame.mode == PlayerPhysicsFrame.Mode.Teleport)
            {
                reject |= RejectionFlags.Position | RejectionFlags.Velocity;
                untrustedFrame.position = authorityFrame.position;
                untrustedFrame.velocity = authorityFrame.velocity;
            }

            //Validate - copy critical features of authority frame over
            if (ValidationUtility.Bound(ref untrustedFrame.position, authorityFrame.position, positionForgiveness)) reject |= RejectionFlags.Position;
            if (ValidationUtility.Bound(ref untrustedFrame.velocity, authorityFrame.velocity, velocityForgiveness)) reject |= RejectionFlags.Velocity;

            //Record
            if (beforeInsert.value.time == untrustedFrame.time) beforeInsert.value = untrustedFrame; //TODO should this use stable ID instead?
            else serverFrameFutures.Insert(beforeInsert, untrustedFrame); //TODO should this overwrite instead?

            //'Untrusted' frame made it through validation, anything before is already valid by extension and therefore irrelevant
            if (reject != 0) RecalcAfter(serverFrameFutures, untrustedFrame.time);
            TrimBefore(serverFrameFutures, untrustedFrame.time);
            kinematics.frame = serverFrameFutures.Tail.value;
            kinematics.transform.position = serverFrameFutures.Tail.value.position; //Force update transform so we can ignore collisions

            //Send frame to ALL players
            DONOTCALL_ReceiveAuthorityFrame_ClientRpc(untrustedFrame, reject);
        }
        else
        {
            //Abort if a more recent basis frame has been validated
            Debug.LogWarning($"Player {src.Receive.SenderClientId} sent a frame ({untrustedFrame.time}) but it was already overwritten ({serverFrameFutures.Peek().time})");
        }
    }

    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    private void DONOTCALL_ReceiveAuthorityFrame_ClientRpc(PlayerPhysicsFrame frame, RejectionFlags reject, ClientRpcParams p = default)
    {
        //if (IsHost) return; //No need to adjust authority copy?

        //Locate relevant frame
        RecyclingNode<PlayerPhysicsFrame> n;
        try
        {
            n = clientFrameHistory.FindNode(IsOwner ? i => frame.id == i.id
                                                    : i => frame.time >= i.time);
        }
        catch (IndexOutOfRangeException e)
        {
            Debug.LogWarning("Duplicate server response for frame at " + frame.time, this);
            return;
        }

        //Apply changes, if any
        if (reject != 0)
        {
            if (reject.HasFlag(RejectionFlags.Position)) n.value.position = frame.position;
            if (reject.HasFlag(RejectionFlags.Velocity)) n.value.velocity = frame.velocity;

            //Recalculate
            Recalc(clientFrameHistory, n.next);
        }
        
        //This frame should now be validated from the server's standpoint, anything before is irrelevant
        TrimBefore(clientFrameHistory, frame.time);

        kinematics.frame = clientFrameHistory.Tail.value;
        kinematics.transform.position = clientFrameHistory.Tail.value.position; //Force update transform so we can ignore collisions
    }
}
