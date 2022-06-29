using System;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Represents a snapshot of a Rigidbody with fixed arc, typically a projectile.
/// </summary>
[Serializable]
public struct KinematicPhysicsFrame : INetworkSerializeByMemcpy
{
    public Vector3 position;
    public Vector3 velocity;
    public float time;

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
            time = Time.realtimeSinceStartup
        };
    }
}