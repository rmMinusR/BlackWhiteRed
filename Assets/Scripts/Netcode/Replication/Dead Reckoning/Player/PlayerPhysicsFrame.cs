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

    //Additional player-specific settings
    public Vector2 look;
    public float slipperiness;
    public float moveSpeed;

    //Input data
    public Vector2 input; //Players have variable acceleration
    public bool jump;

    //Derivative state data
    public bool isGrounded;
    public float timeSinceLastGround;
    public float timeCanNextJump;

    #region Cached expensive math

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

    internal Vector3 Right
    {
        get
        {
            __RefreshLookTrig();
            return new Vector3(__cosLookX, 0, -__sinLookX);
        }
    }

    internal Vector3 Forward
    {
        get
        {
            __RefreshLookTrig();
            return new Vector3(__sinLookX, 0, __cosLookX);
        }
    }

    #endregion
}