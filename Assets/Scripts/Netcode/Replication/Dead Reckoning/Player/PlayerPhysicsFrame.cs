using System;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Represents a snapshot of a player Rigidbody
/// </summary>
[Serializable]
public struct PlayerPhysicsFrame : INetworkSerializeByMemcpy, IPhysicsFrame
{
    //Default kinematics
    [SerializeField] private Vector3 _position;
    [SerializeField] private Vector3 _velocity;
    [SerializeField] private float   _time;

    public Vector3 position {
        [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _position;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] set => _position = value;
    }
    public Vector3 velocity {
        [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _velocity;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] set => _velocity = value;
    }
    public float time {
        [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _time;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] set => _time = value;
    }

    //Additional player-specific stuff
    public Vector2 look;
    public Vector2 input; //Players have variable acceleration
    public float slipperiness;
    public float moveSpeed;

    //Cache expensive math
    private float __lastLookX;
    private float __sinLookX;
    private float __cosLookX;
    private void __RefreshLookTrig()
    {
        if(__lastLookX != look.x || (__lastLookX == 0 && __sinLookX == 0 && __cosLookX == 0))
        {
            __lastLookX = look.x;
            __sinLookX = Mathf.Sin(look.x * Mathf.Deg2Rad);
            __cosLookX = Mathf.Cos(look.x * Mathf.Deg2Rad);
        }
    }

    internal Vector3 LookRight
    {
        get
        {
            __RefreshLookTrig();
            return new Vector3(__cosLookX, 0, __sinLookX);
        }
    }

    internal Vector3 LookForward
    {
        get
        {
            __RefreshLookTrig();
            return new Vector3(-__sinLookX, 0, __cosLookX);
        }
    }

    /*
    /// <summary>
    /// Convenience method to quickly make a physics frame without repeated boilerplate code
    /// </summary>
    /// <param name="rb">Object to snapshot</param>
    /// <returns>Snapshot, in *local* time</returns>
    public static PlayerPhysicsFrame For(Rigidbody rb)
    {
        return new PlayerPhysicsFrame
        {
            position = rb.position,
            velocity = rb.velocity,
            time = Time.realtimeSinceStartup
        };
    }
    // */
}