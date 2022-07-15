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

    private void SubmitFrameToServer()
    {
        //Testing-only, allows use in other scenes without network setup
        //TODO strip
        if (!IsSpawned || !isActiveAndEnabled) return;

        //Push to history
        clientFrameHistory.Enqueue(kinematics.frame);
        TrimBefore(clientFrameHistory); //Trim - TODO use confirmation instead
        DONOTCALL_RecieveClientFrame_ServerRpc(kinematics.frame);
    }

    private static void TrimBefore(RecyclingLinkedQueue<PlayerPhysicsFrame> list, float cutoffTime)
    {
        while (list.Peek().time < cutoffTime) list.Dequeue();
    }

    [Header("Validation (server-side only)")]
    [SerializeField] [Min(0.001f)] private float positionForgiveness = 0.05f;
    [SerializeField] [Min(0.001f)] private float velocityForgiveness = 0.05f;

    [ServerRpc(Delivery = RpcDelivery.Unreliable, RequireOwnership = true)]
    private void DONOTCALL_RecieveClientFrame_ServerRpc(PlayerPhysicsFrame untrustedFrame, ServerRpcParams p = default)
    {
        //Verify ownership
        if (p.Receive.SenderClientId != OwnerClientId) throw new AccessViolationException($"Player {p.Receive.SenderClientId} tried to send physics data as {OwnerClientId}!");

        if (untrustedFrame.time <= serverFrameFutures.Peek().time)
        {
            //int insertIndex = serverFrameHistory.FindLastIndex(i => i.time < untrustedFrame.time);
            //int insertIndex = 1;
            //while (insertIndex < serverFrameFutures.Count && serverFrameFutures[insertIndex].time < untrustedFrame.time) ++insertIndex;
            RecyclingLinkedQueue<PlayerPhysicsFrame>.Node beforeInsert = serverFrameFutures.Head;
            while (beforeInsert != null && beforeInsert.value.time < untrustedFrame.time) beforeInsert = beforeInsert.next;

            //TODO Protect against duplicates

            //Protect against cached-data attack
            if (untrustedFrame.input.sqrMagnitude > 1) untrustedFrame.input.Normalize();
            untrustedFrame.RefreshLookTrig();
            
            //Simulate forward
            PlayerPhysicsFrame prevFrame = beforeInsert.value;
            PlayerPhysicsFrame authorityFrame = kinematics.Step(prevFrame, untrustedFrame.time-prevFrame.time, false);

            //Validate - copy critical features of authority frame over
            untrustedFrame.position = ValidationUtility.Bound(out bool positionInvalid, untrustedFrame.position, authorityFrame.position, positionForgiveness);
            untrustedFrame.velocity = ValidationUtility.Bound(out bool velocityInvalid, untrustedFrame.velocity, authorityFrame.velocity, velocityForgiveness);

            //Record
            serverFrameFutures.Insert(insertIndex, untrustedFrame);
            TrimBefore(serverFrameFutures, untrustedFrame.time); //Trim - TODO use confirmation instead

            //Debug.Log("Rollback triggered", this);
            //if (insertIndex < serverFrameHistory.Count-1) RecalcAllSince(serverFrameHistory, untrustedFrame.time);

            //If any changes were made, issue correction
            if(positionInvalid || velocityInvalid)
            {
                //IssueCorrection();
            }
        }
        else
        {
            //Abort if a more recent basis frame has been validated
            Debug.LogWarning($"Player {p.Receive.SenderClientId} sent a frame ({untrustedFrame.time}) but it was already overwritten ({serverFrameFutures.Peek().time})");
        }
    }

    [Pure]
    private void RecalcAllSince(List<PlayerPhysicsFrame> frameHistory, float sinceTime)
    {
        for (int i = 1; i < frameHistory.Count-1; ++i)
        {
            if (sinceTime < frameHistory[i].time) frameHistory[i+1] = kinematics.Step(frameHistory[i], frameHistory[i+1].time-frameHistory[i].time, false);
        }
    }
}
