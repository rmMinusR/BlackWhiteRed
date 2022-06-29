using System;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Represents a snapshot of a Rigidbody, typically a player or projectile.
/// </summary>
[Serializable]
public struct KinematicPhysicsFrame : INetworkSerializeByMemcpy
{
    public Vector3 position;
    public Vector3 velocity;
    //public Vector3 acceleration; // TODO how to handle non-constant acceleration? How much would this depend on character controller implementation?
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
            //acceleration = Physics.gravity,
            time = Time.realtimeSinceStartup
        };
    }
}