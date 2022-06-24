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

        if (IsClient) {
            _serverFrame.OnValueChanged += ReceiveFrame;
        }
    }

    //TODO is delivery guaranteed or not?
    //Uses SERVER time
    [SerializeField] private NetworkVariable<PhysicsFrame> _serverFrame = new NetworkVariable<PhysicsFrame>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);
    
    //Uses LOCAL time
    [SerializeField] private PhysicsFrame _localFrame;

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
    private void DONOTCALL_SendFrame_ServerRpc(PhysicsFrame newFrame)
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
    }

    private void ReceiveFrame(PhysicsFrame old, PhysicsFrame @new)
    {
        //Convert to local time
        _localFrame = _serverFrame.Value;
        _localFrame.time = NetHeartbeat.Self.ConvertTimeServerToLocal(_serverFrame.Value.time);
    }

    private static float cached_fixedDeltaTime = -1;
    private static float cached_smoothLerpAmt = 1;

    private void FixedUpdate()
    {
        if(!IsOwner)
        {
            if (cached_fixedDeltaTime != Time.fixedDeltaTime)
            {
                cached_fixedDeltaTime = Time.fixedDeltaTime;
                cached_smoothLerpAmt = Mathf.Pow(smoothSharpness, Time.fixedDeltaTime);
            }
            
            PhysicsFrame targetPos = DeadReckoningUtility.DeadReckon(_serverFrame.Value, Time.realtimeSinceStartup);
            
            rb.position = Vector3.Lerp(rb.position, targetPos.position, cached_smoothLerpAmt);
            rb.velocity = Vector3.Lerp(rb.velocity, targetPos.velocity, cached_smoothLerpAmt);
        }
    }
}
