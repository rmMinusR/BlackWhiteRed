using System;
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

    public Vector3 position { get => position; set => position = value; }
    public Vector3 velocity { get => velocity; set => velocity = value; }
    public float   time     { get => time    ; set => time     = value; }

    //Additional player-specific stuff
    public Vector2 look;
    public Vector2 input; //Players have variable acceleration based on input

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