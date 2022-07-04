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

        if (IsServer)
        {
            //Set initial value
            authorityFrame.Value = PlayerPhysicsFrame.For(kinematics, move, look);
        }

        if (!IsLocalPlayer)
        {
            authorityFrame.OnValueChanged -= Callback_CopyRemoteFrame;
            authorityFrame.OnValueChanged += Callback_CopyRemoteFrame;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        authorityFrame.OnValueChanged -= Callback_CopyRemoteFrame; //FIXME is this even necessary?
    }

    #region Owner response and value negotiation

    //TODO is delivery guaranteed or not?
    //Uses SERVER time
    [SerializeField] private NetworkVariable<PlayerPhysicsFrame> authorityFrame = new NetworkVariable<PlayerPhysicsFrame>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);
    private float mostRecentFrame = -1;

    [SerializeField] [Range(0.6f, 1)] private float smoothSharpness = 0.95f;

    private void Owner_FixedUpdate()
    {
        SendFrame(PlayerPhysicsFrame.For(kinematics, move, look));
    }

    private void SendFrame(PlayerPhysicsFrame localFrame) //Under most circumstances, localFrame = PhysicsFrame.For(rb)
    {
        if (!IsLocalPlayer) throw new AccessViolationException();

        PlayerPhysicsFrame serverFrame = localFrame;
        serverFrame.time = (float) NetworkManager.Singleton.ServerTime.FixedTime;
        DONOTCALL_SendFrame_ServerRpc(serverFrame);
    }

    [Header("Validation (server only)")]
    [SerializeField] [Min(0)] private float velocityForgiveness = 0.1f; //Should this be a ratio instead?
    [SerializeField] [Min(0)] private float positionForgiveness = 0.1f;
    [SerializeField] [Min(0)] private float timeForgiveness = 2f;

    /// <summary>
    /// DO NOT CALL DIRECTLY, use SendFrame instead, it will handle time conversion.
    /// </summary>
    /// <param name="newFrame">Physics frame in server's time</param>
    [ServerRpc(Delivery = RpcDelivery.Unreliable, RequireOwnership = true)]
    private void DONOTCALL_SendFrame_ServerRpc(PlayerPhysicsFrame newFrame, ServerRpcParams src = default)
    {
        RejectReason reject = 0;

        //Don't do anything if a more recent frame was already received
        if (newFrame.time < mostRecentFrame) return;

        //Validate time (no major skips and not too far in the future!)
        float timeDiff = newFrame.time - (float)NetworkManager.Singleton.ServerTime.FixedTime;
        if (timeDiff < timeForgiveness*NetHeartbeat.Of(src.Receive.SenderClientId).SmoothedRTT)
        {
            mostRecentFrame = newFrame.time;
            newFrame = PlayerDeadReckoningUtility.DeadReckon(newFrame, (float) NetworkManager.Singleton.ServerTime.FixedTime, proj, transform.rotation);

            //Validate velocity and position
            newFrame.velocity = ValidationUtility.Bound(out bool velocityOutOfBounds, newFrame.velocity, kinematics.velocity, velocityForgiveness); //TODO factor in RTT? Would need to clamp to reasonable bounds.
            if (velocityOutOfBounds) reject |= RejectReason.Velocity;
            newFrame.position = ValidationUtility.Bound(out bool positionOutOfBounds, newFrame.position, transform .position, positionForgiveness);
            if (positionOutOfBounds) reject |= RejectReason.Position;

            //Log to console
            if (velocityOutOfBounds) Debug.LogWarning("Player #" +OwnerClientId+ " accelerated too quickly!");
            if (positionOutOfBounds) Debug.LogWarning("Player #" +OwnerClientId+ " moved too quickly!");

            //Value is within acceptable bounds, apply
            authorityFrame.Value = newFrame;
        }
        else
        {
            reject |= RejectReason.Time;
            Debug.Log("Player #"+OwnerClientId+" sent a packet with wrong time! "+timeDiff+" sec ahead", this);
        }

        if(reject != 0) FrameRejected_ClientRpc(authorityFrame.Value, reject, src.ReturnToSender());
    }
    
    [Flags]
    private enum RejectReason
    {
        Position = 1 << 0,
        Velocity = 1 << 1,
        Time = 1 << 2
    }

    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    private void FrameRejected_ClientRpc(PlayerPhysicsFrame @new, RejectReason rejectReason, ClientRpcParams p = default)
    {
        if (!IsLocalPlayer) throw new AccessViolationException("Only owner can recieve reject messages! Use "+nameof(Callback_CopyRemoteFrame)+" instead.");

        string rejectInfo = "REJECTED ("+rejectReason+") t="+(float)NetworkManager.Singleton.ServerTime.FixedTime+"/"+@new.time+" d"+((float)NetworkManager.Singleton.ServerTime.FixedTime-@new.time);

        @new = PlayerDeadReckoningUtility.DeadReckon(@new, (float) NetworkManager.Singleton.ServerTime.FixedTime, proj, transform.rotation);
        
        rejectInfo += " pos="+Vector3.Distance(transform .position, @new.position)
                   +  " vel="+Vector3.Distance(kinematics.velocity, @new.velocity);
        Debug.Log(rejectInfo, this);

        //TODO should this be in FixedUpdate?
        if (rejectReason.HasFlag(RejectReason.Position)) transform .position = @new.position;
        if (rejectReason.HasFlag(RejectReason.Velocity)) kinematics.velocity = @new.velocity;
    }

    #endregion

    #region Non-owner response

    private (Vector3 pos, Vector3 vel)? lastKnownRemoteFrame;
    private void Callback_CopyRemoteFrame(PlayerPhysicsFrame _, PlayerPhysicsFrame @new)
    {
        if (IsLocalPlayer) throw new InvalidOperationException("Owner should use "+nameof(FrameRejected_ClientRpc)+" instead");

        @new = PlayerDeadReckoningUtility.DeadReckon(@new, (float) NetworkManager.Singleton.ServerTime.FixedTime, proj, transform.rotation);

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
        PlayerPhysicsFrame targetPos = PlayerDeadReckoningUtility.DeadReckon(authorityFrame.Value, (float) NetworkManager.Singleton.ServerTime.FixedTime, proj, transform.rotation);

        //Exponential decay lerp towards correct position
        transform .position = Vector3.Lerp(transform .position, targetPos.position, cached_smoothLerpAmt);
        kinematics.velocity = Vector3.Lerp(kinematics.velocity, targetPos.velocity, cached_smoothLerpAmt);
    }

    #endregion
    
    private void FixedUpdate()
    {
        if (IsLocalPlayer) Owner_FixedUpdate();
        else if (IsClient) NonOwner_FixedUpdate();
    }
}
