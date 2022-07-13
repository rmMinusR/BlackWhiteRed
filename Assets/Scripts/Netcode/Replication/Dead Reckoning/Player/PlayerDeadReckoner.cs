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

    }

    [ServerRpc(Delivery = RpcDelivery.Unreliable, RequireOwnership = true)]
    private void RecieveClientFrame_ServerRpc(PlayerPhysicsFrame frame, ServerRpcParams p = default)
    {
        int insertIndex = clientFrameHistory.FindLastIndex(x => x.time < frame.time) - 1;

        //Validate
        PlayerPhysicsFrame prevFrame = clientFrameHistory[insertIndex-1];
        kinematics.Step(prevFrame, frame.time-prevFrame.time, false);
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
