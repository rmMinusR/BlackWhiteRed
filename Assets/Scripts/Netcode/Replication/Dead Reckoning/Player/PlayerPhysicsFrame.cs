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
    public Type type;

    //Look is similar to input, but never rejected by validation. Can be overwritten by Teleport, and can indicate aimbotting.
    [SerializeField] private Vector2 _look;
    public Vector2 look {
        get => _look;
        set
        {
            _look = value;
            __lookTrigDirty = true;
        }
    }

    [Serializable] public struct Input : INetworkSerializeByMemcpy
    {
        public Vector2 move; //Raw input, trusted if length < 1
        public bool jump; //Raw input, always trusted
    }
    public Input input;

    //Derivative state data. Never trust player copy.
    public bool isGrounded;
    public float timeSinceLastGround;
    public float timeCanNextJump;

    [Flags]
    public enum Type
    {
        NormalMove,
        Teleport,

        Default = NormalMove
    }

    public static bool DoCollisionTest(Type m)
    {
        return m != Type.Teleport;
    }

    #region Cached expensive math

    private bool __lookTrigDirty;
    private float __sinLookX;
    private float __cosLookX;
    public void RefreshLookTrig()
    {
        __sinLookX = Mathf.Sin(look.x * Mathf.Deg2Rad);
        __cosLookX = Mathf.Cos(look.x * Mathf.Deg2Rad);
        __lookTrigDirty = false;
    }

    internal Vector3 Right
    {
        get
        {
            if (__lookTrigDirty) RefreshLookTrig();
            return new Vector3(__cosLookX, 0, -__sinLookX);
        }
    }

    internal Vector3 Forward
    {
        get
        {
            if (__lookTrigDirty) RefreshLookTrig();
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
        serializer.SerializeValue(ref type);

        serializer.SerializeValue(ref input);
    }
}