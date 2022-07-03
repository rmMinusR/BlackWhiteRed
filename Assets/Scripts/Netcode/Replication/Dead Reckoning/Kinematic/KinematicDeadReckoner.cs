﻿using System;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public sealed class KinematicDeadReckoner : NetworkBehaviour
{
    private Rigidbody rb;
    private ProjectionShape proj;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        rb = GetComponent<Rigidbody>();
        proj = ProjectionShape.Build(gameObject);

        if (!IsOwner)
        {
            _serverFrame.OnValueChanged -= Callback_CopyRemoteFrame;
            _serverFrame.OnValueChanged += Callback_CopyRemoteFrame;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        _serverFrame.OnValueChanged -= Callback_CopyRemoteFrame; //FIXME is this even necessary?
    }

    #region Owner response and value negotiation

    //TODO is delivery guaranteed or not?
    //Uses SERVER time
    [SerializeField] private NetworkVariable<KinematicPhysicsFrame> _serverFrame = new NetworkVariable<KinematicPhysicsFrame>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);

    [SerializeField] [Range(0.6f, 1)] private float smoothSharpness = 0.95f;

    private void SendFrame(KinematicPhysicsFrame localFrame) //Under most circumstances, localFrame = PhysicsFrame.For(rb)
    {
        if (!IsOwner) throw new AccessViolationException();

        KinematicPhysicsFrame serverFrame = localFrame;
        serverFrame.time = NetHeartbeat.Self.ConvertTimeLocalToServer(serverFrame.time);
        DONOTCALL_SendFrame_ServerRpc(serverFrame);
    }

    [Header("Validation (server only)")]
    [SerializeField] [Min(0)] private float velocityForgiveness = 0.1f; //Should this be a ratio instead?
    [SerializeField] [Min(0)] private float positionForgiveness = 0.1f;

    /// <summary>
    /// DO NOT CALL DIRECTLY, use SendFrame instead, it will handle time conversion.
    /// </summary>
    /// <param name="newFrame">Physics frame in server's time</param>
    [ServerRpc(Delivery = RpcDelivery.Unreliable, RequireOwnership = true)]
    private void DONOTCALL_SendFrame_ServerRpc(KinematicPhysicsFrame newFrame, ServerRpcParams src = default)
    {
        //Validate time (no major skips and not in the future!)
        if (Mathf.Abs(_serverFrame.Value.time-newFrame.time) > 2*NetHeartbeat.Of(src.Receive.SenderClientId).SmoothedRTT)
        {
            FrameRejected_ClientRpc(_serverFrame.Value, true, true, src.ReturnToSender());
            return;
        }
        
        KinematicPhysicsFrame currentValAtNewTime = KinematicDeadReckoningUtility.DeadReckon(_serverFrame.Value, newFrame.time, proj, transform.rotation);

        //Validate velocity and position
        newFrame.velocity = ValidationUtility.Bound(out bool velocityOutOfBounds, newFrame.velocity, currentValAtNewTime.velocity, velocityForgiveness); //TODO factor in RTT? Would need to clamp to reasonable bounds.
        newFrame.position = ValidationUtility.Bound(out bool positionOutOfBounds, newFrame.position, currentValAtNewTime.position, positionForgiveness);
        if (velocityOutOfBounds) Debug.LogWarning(gameObject.name + " experienced too much acceleration!");
        if (positionOutOfBounds) Debug.LogWarning(gameObject.name + " moved too quickly!");

        //Value is within acceptable bounds, apply
        _serverFrame.Value = newFrame;

        if (velocityOutOfBounds || positionOutOfBounds)
        {
            FrameRejected_ClientRpc(newFrame, velocityOutOfBounds, positionOutOfBounds, src.ReturnToSender());
        }
    }

    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    private void FrameRejected_ClientRpc(KinematicPhysicsFrame @new, bool rejectedVelocity, bool rejectedPosition, ClientRpcParams p = default)
    {
        if (!IsOwner) throw new AccessViolationException();

        @new.time = NetHeartbeat.Self.ConvertTimeServerToLocal(@new.time);
        @new = KinematicDeadReckoningUtility.DeadReckon(@new, Time.realtimeSinceStartup, proj, transform.rotation);

        //TODO should this be in FixedUpdate?
        if (rejectedPosition) rb.position = @new.position;
        if (rejectedVelocity) rb.velocity = @new.velocity;
    }

    private void Owner_FixedUpdate()
    {
        SendFrame(KinematicPhysicsFrame.For(rb));
    }

    #endregion

    #region Non-owner response

    private (Vector3 pos, Vector3 vel)? lastKnownRemoteFrame;
    private void Callback_CopyRemoteFrame(KinematicPhysicsFrame _, KinematicPhysicsFrame @new)
    {
        if (!IsOwner) throw new InvalidOperationException("Owner should use "+nameof(FrameRejected_ClientRpc)+" instead");

        @new.time = NetHeartbeat.Self.ConvertTimeServerToLocal(@new.time);
        @new = KinematicDeadReckoningUtility.DeadReckon(@new, Time.realtimeSinceStartup, proj, transform.rotation);

        //TODO should this be in FixedUpdate?
        rb.MovePosition(@new.position);
        rb.velocity = @new.velocity;
    }

    private static float cached_fixedDeltaTime = -1;
    private static float cached_smoothLerpAmt = 1;

    private void NonOwner_FixedUpdate()
    {
        if (cached_fixedDeltaTime != Time.fixedDeltaTime)
        {
            cached_fixedDeltaTime = Time.fixedDeltaTime;
            cached_smoothLerpAmt = Mathf.Pow(smoothSharpness, Time.fixedDeltaTime);
        }

        //FIXME expensive, run only on value change?
        KinematicPhysicsFrame targetPos = KinematicDeadReckoningUtility.DeadReckon(_serverFrame.Value, Time.realtimeSinceStartup, proj, transform.rotation);

        //Exponential decay lerp towards correct position
        rb.MovePosition(Vector3.Lerp(rb.position, targetPos.position, cached_smoothLerpAmt));
        rb.velocity = Vector3.Lerp(rb.velocity, targetPos.velocity, cached_smoothLerpAmt);
    }

    #endregion
    
    private void FixedUpdate()
    {
        if (IsOwner) Owner_FixedUpdate();
        else NonOwner_FixedUpdate();
    }
}
