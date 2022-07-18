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

        if (IsLocalPlayer || IsServer)
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
    /*[SerializeField]/* */ private RecyclingLinkedQueue<PlayerPhysicsFrame> serverFrameFutures = new RecyclingLinkedQueue<PlayerPhysicsFrame>();
    /*[SerializeField]/* */ private RecyclingLinkedQueue<PlayerPhysicsFrame> clientFrameHistory = new RecyclingLinkedQueue<PlayerPhysicsFrame>();

    private static void TrimBefore(RecyclingLinkedQueue<PlayerPhysicsFrame> list, float cutoffTime)
    {
        while (list.Head != null && list.Head.value.time < cutoffTime) list.DropHead();
    }

    [Flags]
    private enum RejectionFlags
    {
        Position = (1 << 0),
        Velocity = (1 << 1)
    }

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

        Debug.Log("Checkpoint A, frame #"+Time.frameCount);

        if (serverFrameFutures.Peek().time <= untrustedFrame.time)
        {
            Debug.Log("Checkpoint B");

            //Find insert position
            RecyclingLinkedQueue<PlayerPhysicsFrame>.Node beforeInsert = serverFrameFutures.Head;
            while (beforeInsert != null && beforeInsert.value.time < untrustedFrame.time) beforeInsert = beforeInsert.next;

            Debug.Log("Checkpoint C");

            //Protect against cached-data attack
            if (untrustedFrame.input.sqrMagnitude > 1) untrustedFrame.input.Normalize();
            untrustedFrame.RefreshLookTrig();

            //Simulate forward
            PlayerPhysicsFrame prevFrame = beforeInsert.value;
            PlayerPhysicsFrame authorityFrame = kinematics.Step(prevFrame, untrustedFrame.time-prevFrame.time, false);
            if (beforeInsert != serverFrameFutures.Tail) RecalcAfter(serverFrameFutures, untrustedFrame.time);

            Debug.Log("Checkpoint D");

            //Validate - copy critical features of authority frame over
            untrustedFrame.position = ValidationUtility.Bound(out bool positionInvalid, untrustedFrame.position, authorityFrame.position, positionForgiveness);
            untrustedFrame.velocity = ValidationUtility.Bound(out bool velocityInvalid, untrustedFrame.velocity, authorityFrame.velocity, velocityForgiveness);

            Debug.Log("Checkpoint E");

            Debug.Log("F mode "+(beforeInsert.value.time == untrustedFrame.time ? "OVERWRITE" : "INSERT"));

            //Record
            if (beforeInsert.value.time == untrustedFrame.time) beforeInsert.value = untrustedFrame;
            else serverFrameFutures.Insert(beforeInsert, untrustedFrame); //TODO should this overwrite instead?

            Debug.Log("Checkpoint F");

            //'Untrusted' frame was made it through validation, anything before is already valid by extension and therefore irrelevant
            TrimBefore(serverFrameFutures, untrustedFrame.time);

            Debug.Log("Checkpoint G");

            //Send response verifying or rejecting
            RejectionFlags reject = 0;
            if (positionInvalid) reject |= RejectionFlags.Position;
            if (velocityInvalid) reject |= RejectionFlags.Velocity;
            if (reject != 0) RecalcAfter(serverFrameFutures, untrustedFrame.time);

            Debug.Log("Checkpoint H");

            DONOTCALL_ReceiveAuthorityFrame_ClientRpc(untrustedFrame, reject); //Broadcast to ALL players
        }
        else
        {
            //Abort if a more recent basis frame has been validated
            Debug.LogWarning($"Player {src.Receive.SenderClientId} sent a frame ({untrustedFrame.time}) but it was already overwritten ({serverFrameFutures.Peek().time})");
        }

        Debug.Log("Leaving "+nameof(DONOTCALL_ReceiveClientFrame_ServerRpc));
    }

    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    private void DONOTCALL_ReceiveAuthorityFrame_ClientRpc(PlayerPhysicsFrame frame, RejectionFlags reject, ClientRpcParams p = default)
    {
        Debug.Log("Checkpoint I");

        if (IsHost) return; //No need to adjust authority copy?

        if (IsLocalPlayer)
        {
            Debug.Log("Checkpoint J");

            //Apply changes, if any
            if (reject != 0)
            {
                Debug.Log("Checkpoint K");

                //Locate relevant frame
                RecyclingLinkedQueue<PlayerPhysicsFrame>.Node n = clientFrameHistory.FindNode(i => frame.time == i.time);
                if (n == null) //Ignore if duplicate (already handled)
                {
                    Debug.LogWarning("Duplicate server response for frame at "+frame.time, this);
                    return;
                }

                Debug.Log("Checkpoint L");

                if (reject.HasFlag(RejectionFlags.Position)) n.value.position = frame.position;
                if (reject.HasFlag(RejectionFlags.Velocity)) n.value.velocity = frame.velocity;

                //Recalculate
                RecalcAfter(clientFrameHistory, frame.time);

                Debug.Log("Checkpoint M");
            }

            //This frame should now be validated with the server's copy, anything before is irrelevant
            TrimBefore(clientFrameHistory, frame.time);

            Debug.Log("Checkpoint N");
        }
        else
        {
            //TODO be more specific?
            //TODO penetrate surfaces?
            kinematics.frame = frame;
        }
    }
}
