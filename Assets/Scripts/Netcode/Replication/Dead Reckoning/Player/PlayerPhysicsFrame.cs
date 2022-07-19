using System;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Represents a snapshot of a player Rigidbody
/// </summary>
[Serializable]
public struct PlayerPhysicsFrame : INetworkSerializable
{
    //Default kinematics
    public Vector3 position;
    public Vector3 velocity;
    public float   time;

    public uint id; //Ensure time-adjustment parity
    public Mode mode;

    //Additional player-specific settings
    public Vector2 look;

    //Input data
    public Vector2 input; //Players have variable acceleration
    public bool jump;

    //Derivative state data
    public bool isGrounded;
    public float timeSinceLastGround;
    public float timeCanNextJump;

    [Flags]
    public enum Mode
    {
        Default = NormalMove,

        NormalMove = (1 << 0),
        Teleport   = (1 << 1)
    }

    #region Cached expensive math

    private float __lastLookX;
    private float __sinLookX;
    private float __cosLookX;
    public void RefreshLookTrig()
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
            RefreshLookTrig();
            return new Vector3(__cosLookX, 0, -__sinLookX);
        }
    }

    internal Vector3 Forward
    {
        get
        {
            RefreshLookTrig();
            return new Vector3(__sinLookX, 0, __cosLookX);
        }
    }

    #endregion

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref position);
        serializer.SerializeValue(ref velocity);
        serializer.SerializeValue(ref time);

        serializer.SerializeValue(ref id);
        serializer.SerializeValue(ref mode);

        serializer.SerializeValue(ref look);
        serializer.SerializeValue(ref input);
        serializer.SerializeValue(ref jump);
    }
}