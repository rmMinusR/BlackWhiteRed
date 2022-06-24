using System;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public sealed class SimpleDeadReckonedTransform : NetworkBehaviour
{
    private Rigidbody rb;
    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        if(!IsOwner)
        {
            _serverFrame.OnValueChanged -= NonOwner_CopyRemoteFrame;
            _serverFrame.OnValueChanged += NonOwner_CopyRemoteFrame;
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        _serverFrame.OnValueChanged -= NonOwner_CopyRemoteFrame; //FIXME is this even necessary?
    }

    //TODO is delivery guaranteed or not?
    //Uses SERVER time
    [SerializeField] private NetworkVariable<PhysicsFrame> _serverFrame = new NetworkVariable<PhysicsFrame>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);
    
    [SerializeField] [Range(0.6f, 1)] private float smoothSharpness = 0.95f;

    private void SendFrame(PhysicsFrame localFrame) //Under most circumstances, localFrame = PhysicsFrame.For(rb)
    {
        if (!IsOwner) throw new AccessViolationException();

        PhysicsFrame serverFrame = localFrame;
        serverFrame.time = NetHeartbeat.Self.ConvertTimeLocalToServer(serverFrame.time);
        DONOTCALL_SendFrame_ServerRpc(serverFrame);
    }

    [Header("Validation (server only)")]
    [SerializeField] [Min(0)] private float velocityForgiveness = 0.1f; //Should this be a ratio instead?
    [SerializeField] [Min(0)] private float positionForgiveness = 0.1f;

    /// <summary>
    /// DO NOT CALL DIRECTLY, use SendFrame instead, it will handle time conversion.
    /// </summary>
    /// <param name="newFrame"></param>
    [ServerRpc(Delivery = RpcDelivery.Unreliable, RequireOwnership = true)]
    private void DONOTCALL_SendFrame_ServerRpc(PhysicsFrame newFrame, ServerRpcParams src = default)
    {
        //TODO Validate time (no major skips and not in the future!)

        PhysicsFrame currentValAtNewTime = DeadReckoningUtility.DeadReckon(_serverFrame.Value, newFrame.time);

        //Validate velocity and position
        ValidationUtility.Bound(out bool velocityOutOfBounds, ref newFrame.velocity, currentValAtNewTime.velocity, velocityForgiveness); //TODO factor in RTT? Would need to clamp to reasonable bounds.
        ValidationUtility.Bound(out bool positionOutOfBounds, ref newFrame.position, currentValAtNewTime.position, positionForgiveness);
        if (velocityOutOfBounds) Debug.LogWarning(gameObject.name+" experienced too much acceleration!");
        if (positionOutOfBounds) Debug.LogWarning(gameObject.name+" moved too quickly!");
        
        //Value is within acceptable bounds, apply
        _serverFrame.Value = newFrame;

        if (velocityOutOfBounds || positionOutOfBounds)
        {
            FrameRejected_ClientRpc(newFrame, velocityOutOfBounds, positionOutOfBounds, src.ReturnToSender());
        }
    }

    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    private void FrameRejected_ClientRpc(PhysicsFrame @new, bool rejectedVelocity, bool rejectedPosition, ClientRpcParams p = default)
    {
        if (!IsOwner) throw new AccessViolationException();

        @new.time = NetHeartbeat.Self.ConvertTimeServerToLocal(@new.time);
        @new = DeadReckoningUtility.DeadReckon(@new, Time.realtimeSinceStartup);

        //TODO should this be in FixedUpdate?
        if (rejectedPosition) rb.position = @new.position;
        if (rejectedVelocity) rb.velocity = @new.velocity;
    }

    private void NonOwner_CopyRemoteFrame(PhysicsFrame _, PhysicsFrame @new)
    {
        if (!IsOwner) throw new NotImplementedException("Use "+nameof(FrameRejected_ClientRpc)+" instead");

        @new.time = NetHeartbeat.Self.ConvertTimeServerToLocal(@new.time);
        @new = DeadReckoningUtility.DeadReckon(@new, Time.realtimeSinceStartup);

        //TODO should this be in FixedUpdate?
        rb.MovePosition(@new.position);
        rb.velocity = @new.velocity;
    }

    private static float cached_fixedDeltaTime = -1;
    private static float cached_smoothLerpAmt = 1;

    private void FixedUpdate()
    {
        if (IsOwner) SendFrame(PhysicsFrame.For(rb));
        else
        {
            if (cached_fixedDeltaTime != Time.fixedDeltaTime)
            {
                cached_fixedDeltaTime = Time.fixedDeltaTime;
                cached_smoothLerpAmt = Mathf.Pow(smoothSharpness, Time.fixedDeltaTime);
            }
            
            PhysicsFrame targetPos = DeadReckoningUtility.DeadReckon(_serverFrame.Value, Time.realtimeSinceStartup);
            
            //Exponential decay lerp towards correct position
            rb.MovePosition(Vector3.Lerp(rb.position, targetPos.position, cached_smoothLerpAmt));
            rb.velocity = Vector3.Lerp(rb.velocity, targetPos.velocity, cached_smoothLerpAmt);
        }
    }
}
