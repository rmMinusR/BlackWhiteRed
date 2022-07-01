using System;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(CharacterKinematics))]
public sealed class PlayerDeadReckoner : NetworkBehaviour
{
    private ProjectionShape proj;
    private CharacterKinematics kinematics;
    [SerializeField] private PlayerMoveController move;
    [SerializeField] private PlayerLookController look;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        proj = ProjectionShape.Build(gameObject);
        kinematics = GetComponent<CharacterKinematics>();
        
        if (!IsLocalPlayer)
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
    [SerializeField] private NetworkVariable<PlayerPhysicsFrame> _serverFrame = new NetworkVariable<PlayerPhysicsFrame>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);

    [SerializeField] [Range(0.6f, 1)] private float smoothSharpness = 0.95f;

    private void Owner_FixedUpdate()
    {
        SendFrame(PlayerPhysicsFrame.For(kinematics, move, look));
    }

    private void SendFrame(PlayerPhysicsFrame localFrame) //Under most circumstances, localFrame = PhysicsFrame.For(rb)
    {
        if (!IsLocalPlayer) throw new AccessViolationException();

        PlayerPhysicsFrame serverFrame = localFrame;
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
    private void DONOTCALL_SendFrame_ServerRpc(PlayerPhysicsFrame newFrame, ServerRpcParams src = default)
    {
        //Validate time (no major skips and not too far in the future!)
        if (newFrame.time-Time.realtimeSinceStartup > 2*NetHeartbeat.Of(src.Receive.SenderClientId).SmoothedRTT)
        {
            Debug.Log("Serverside: TIME REJECT", this);
            FrameRejected_ClientRpc(_serverFrame.Value, true, true, src.ReturnToSender());
            return;
        }

        PlayerPhysicsFrame currentValAtNewTime = PlayerDeadReckoningUtility.DeadReckon(_serverFrame.Value, newFrame.time, proj, transform.rotation);

        //Validate velocity and position
        newFrame.velocity = ValidationUtility.Bound(out bool velocityOutOfBounds, newFrame.velocity, currentValAtNewTime.velocity, velocityForgiveness); //TODO factor in RTT? Would need to clamp to reasonable bounds.
        newFrame.position = ValidationUtility.Bound(out bool positionOutOfBounds, newFrame.position, currentValAtNewTime.position, positionForgiveness);
        if (velocityOutOfBounds) Debug.LogWarning("Player #" +OwnerClientId+ " experienced too much acceleration!");
        if (positionOutOfBounds) Debug.LogWarning("Player #" +OwnerClientId+ " moved too quickly!");

        //Value is within acceptable bounds, apply
        _serverFrame.Value = newFrame;

        if (velocityOutOfBounds || positionOutOfBounds)
        {
            FrameRejected_ClientRpc(newFrame, velocityOutOfBounds, positionOutOfBounds, src.ReturnToSender());
        }
    }

    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    private void FrameRejected_ClientRpc(PlayerPhysicsFrame @new, bool rejectedVelocity, bool rejectedPosition, ClientRpcParams p = default)
    {
        if (!IsLocalPlayer) throw new AccessViolationException();

        Debug.Log("Clientside: REJECTED "+(rejectedPosition?"pos ":"")+(rejectedVelocity?"vel ":"")+"(server time = "+@new.time+")", this);

        @new.time = NetHeartbeat.Self.ConvertTimeServerToLocal(@new.time);
        @new = PlayerDeadReckoningUtility.DeadReckon(@new, Time.realtimeSinceStartup, proj, transform.rotation);

        //TODO should this be in FixedUpdate?
        if (rejectedPosition) transform .position = @new.position;
        if (rejectedVelocity) kinematics.velocity = @new.velocity;
    }

    #endregion

    #region Non-owner response

    private (Vector3 pos, Vector3 vel)? lastKnownRemoteFrame;
    private void Callback_CopyRemoteFrame(PlayerPhysicsFrame _, PlayerPhysicsFrame @new)
    {
        if (!IsLocalPlayer) throw new InvalidOperationException("Owner should use "+nameof(FrameRejected_ClientRpc)+" instead");

        @new.time = NetHeartbeat.Self.ConvertTimeServerToLocal(@new.time);
        @new = PlayerDeadReckoningUtility.DeadReckon(@new, Time.realtimeSinceStartup, proj, transform.rotation);

        transform .position = @new.position;
        kinematics.velocity = @new.velocity;
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
        PlayerPhysicsFrame targetPos = PlayerDeadReckoningUtility.DeadReckon(_serverFrame.Value, Time.realtimeSinceStartup, proj, transform.rotation);

        //Exponential decay lerp towards correct position
        transform .position = Vector3.Lerp(transform .position, targetPos.position, cached_smoothLerpAmt);
        kinematics.velocity = Vector3.Lerp(kinematics.velocity, targetPos.velocity, cached_smoothLerpAmt);
    }

    #endregion
    
    private void FixedUpdate()
    {
        if (IsLocalPlayer) Owner_FixedUpdate();
        else NonOwner_FixedUpdate();
    }
}
