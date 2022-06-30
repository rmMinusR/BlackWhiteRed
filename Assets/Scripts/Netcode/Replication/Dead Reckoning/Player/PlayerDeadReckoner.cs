using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PlayerDeadReckoner
{
    /// <summary>
    /// Dead reckon using preferred method. Considers collisions.
    /// </summary>
    /// <param name="current">Current frame</param>
    /// <param name="targetTime">Time to simulate until</param>
    /// <param name="shape">Representation of colliders. See ProjectionShape.Build</param>
    /// <param name="rotation"></param>
    /// <returns>Predicted frame at the given time, assuming no collisions</returns>
    public static PlayerPhysicsFrame DeadReckon(PlayerPhysicsFrame current, float targetTime, ProjectionShape shape, Quaternion rotation)
    {
        return DeadReckoningCollisionHandler<PlayerPhysicsFrame>.DeadReckon(current, targetTime, shape, rotation, RawDeadReckon);
    }

    /// <summary>
    /// Alias to preferred dead reckoning method. Does NOT consider collisions.
    /// Current: 2nd degree
    /// </summary>
    /// <param name="current">Current frame</param>
    /// <param name="targetTime">Time to predict</param>
    /// <returns>Predicted frame at the given time, assuming no collisions</returns>
    private static PlayerPhysicsFrame RawDeadReckon(PlayerPhysicsFrame current, float targetTime) => RawDeadReckonDeg2(current, targetTime);

    /// <summary>
    /// 1st degree dead reckoning. Only considers current velocity. Does NOT consider collisions.
    /// </summary>
    /// <param name="current">Current frame</param>
    /// <param name="targetTime">Time to predict</param>
    /// <returns>Predicted frame at the given time, assuming no collisions</returns>
    public static PlayerPhysicsFrame RawDeadReckonDeg1(PlayerPhysicsFrame current, float targetTime)
    {
        float dt = targetTime - current.time;

        if (dt < 0) Debug.LogWarning("Rewinding time is strongly discouraged! (dt="+dt+")");
        
        return new PlayerPhysicsFrame()
        {
            position = current.position + current.velocity*dt, // s = ut //TODO factor in input
            velocity = current.velocity, //TODO factor in input
            time = targetTime,

            input = current.input,
            look = current.look
        };
    }

    /// <summary>
    /// 2nd degree dead reckoning. Considers both velocity and gravity. Does NOT consider collisions.
    /// </summary>
    /// <param name="current">Current position, velocity, and time</param>
    /// <param name="targetTime">Time to predict</param>
    /// <returns>Predicted position and velocity at the given time</returns>
    public static PlayerPhysicsFrame RawDeadReckonDeg2(PlayerPhysicsFrame current, float targetTime)
    {
        float dt = targetTime - current.time;

        if (dt < 0) Debug.LogWarning("Rewinding time is strongly discouraged! (dt="+dt+")");
        
        return new PlayerPhysicsFrame()
        {
            position = current.position + current.velocity*dt + 1/2*Physics.gravity*dt*dt, // s = ut + 1/2 at^2 //TODO factor in input
            velocity = current.velocity + Physics.gravity*dt, // v = u + at //TODO factor in input
            time = targetTime,

            input = current.input,
            look = current.look
        };
    }
}
