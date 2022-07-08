using System;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Represents a snapshot of a Rigidbody with fixed arc, typically a projectile.
/// </summary>
[Serializable]
public struct KinematicPhysicsFrame : INetworkSerializeByMemcpy, IPhysicsFrame
{
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

    /// <summary>
    /// Convenience method to quickly make a physics frame without repeated boilerplate code
    /// </summary>
    /// <param name="rb">Object to snapshot</param>
    /// <returns>Snapshot, in *local* time</returns>
    public static KinematicPhysicsFrame For(Rigidbody rb)
    {
        return new KinematicPhysicsFrame
        {
            position = rb.position,
            velocity = rb.velocity,
            time = (float) NetworkManager.Singleton.ServerTime.FixedTime
        };
    }
}