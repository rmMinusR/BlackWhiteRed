using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(CharacterKinematics))]
public sealed class PlayerDeadReckoner : NetworkBehaviour
{
    private ProjectionShape proj;
    private CharacterKinematics kinematics;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        proj = ProjectionShape.Build(gameObject);
        kinematics = GetComponent<CharacterKinematics>();
        Debug.Assert(proj != null);
        Debug.Assert(kinematics != null);

        clientFrameHistory.Clear();
        clientFrameHistory.Add(kinematics.frame);

        if (IsServer)
        {
            //Set initial value
            serverFrameHistory.Clear();
            serverFrameHistory.Add(kinematics.frame);
        }

        if (IsLocalPlayer)
        {
            kinematics.FinalizeMove += SubmitFrameToServer;
        }
    }

    private void SubmitFrameToServer()
    {
        //Push to history
        clientFrameHistory.Add(kinematics.frame);
        DONOTCALL_RecieveClientFrame_ServerRpc(kinematics.frame);
    }

    [Header("Validation (server-side only)")]
    [SerializeField] [Min(0.001f)] private float positionForgiveness = 0.05f;
    [SerializeField] [Min(0.001f)] private float velocityForgiveness = 0.05f;

    [ServerRpc(Delivery = RpcDelivery.Unreliable, RequireOwnership = true)]
    private void DONOTCALL_RecieveClientFrame_ServerRpc(PlayerPhysicsFrame untrustedFrame, ServerRpcParams p = default)
    {
        //Verify ownership
        if (p.Receive.SenderClientId != OwnerClientId) throw new AccessViolationException($"Player {p.Receive.SenderClientId} tried to send physics data as {OwnerClientId}!");

        if (untrustedFrame.time > serverFrameHistory[0].time)
        {
            int insertIndex = serverFrameHistory.FindIndex(x => x.time > untrustedFrame.time);
            if (insertIndex != -1) --insertIndex; //Found something
            else insertIndex = serverFrameHistory.Count; //Didn't find anything, put at end

            //Simulate
            PlayerPhysicsFrame prevFrame = clientFrameHistory[insertIndex-1];
            PlayerPhysicsFrame authorityFrame = kinematics.Step(prevFrame, untrustedFrame.time-prevFrame.time, false);

            //Validate
            untrustedFrame.position = ValidationUtility.Bound(out bool positionInvalid, untrustedFrame.position, authorityFrame.position, positionForgiveness);
            untrustedFrame.velocity = ValidationUtility.Bound(out bool velocityInvalid, untrustedFrame.velocity, authorityFrame.velocity, velocityForgiveness);

            //Record
            serverFrameHistory.Insert(insertIndex, untrustedFrame);

            //If any changes were made, issue correction
            if(positionInvalid || velocityInvalid)
            {
                if (insertIndex < serverFrameHistory.Count-1) RecalcAllSince(serverFrameHistory, untrustedFrame.time);
                IssueCorrection();
            }
        }
        else
        {
            //Abort if a more recent basis frame has been validated
            Debug.LogWarning($"Player {p.Receive.SenderClientId} sent a frame ({untrustedFrame.time}) but it was already overwritten ({serverFrameHistory[0].time})");
        }
    }

    private List<PlayerPhysicsFrame> serverFrameHistory = new List<PlayerPhysicsFrame>();
    private List<PlayerPhysicsFrame> clientFrameHistory = new List<PlayerPhysicsFrame>();

    [Pure]
    private void RecalcAllSince(List<PlayerPhysicsFrame> frameHistory, float time)
    {
        for (int i = frameHistory.FindIndex(x => x.time < time); i < frameHistory.Count - 2; ++i)
        {
            frameHistory[i+1] = kinematics.Step(frameHistory[i], Time.fixedDeltaTime, false);
        }
    }
}
