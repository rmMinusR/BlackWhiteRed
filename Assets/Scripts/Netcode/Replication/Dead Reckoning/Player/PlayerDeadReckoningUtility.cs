using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PlayerDeadReckoningUtility
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
    /// Current: 3nd degree
    /// </summary>
    /// <param name="current">Current frame</param>
    /// <param name="targetTime">Time to predict</param>
    /// <returns>Predicted frame at the given time, assuming no collisions</returns>
    private static PlayerPhysicsFrame RawDeadReckon(PlayerPhysicsFrame current, float targetTime) => RawDeadReckonDeg3(current, targetTime);

    /// <summary>
    /// 1st degree dead reckoning. Only considers current velocity. Does NOT consider collisions.
    /// </summary>
    /// <param name="current">Current frame</param>
    /// <param name="targetTime">Time to predict</param>
    /// <returns>Predicted frame at the given time, assuming no collisions</returns>
    private static PlayerPhysicsFrame RawDeadReckonDeg1(PlayerPhysicsFrame current, float targetTime)
    {
        float dt = targetTime - current.time;

        if (dt < 0) Debug.LogWarning("Rewinding time is strongly discouraged! (dt="+dt+")");

        current.position += current.velocity*dt;
        current.time = targetTime;

        return current;
    }

    /// <summary>
    /// 2nd degree dead reckoning. Considers both velocity and gravity, but not input. Does NOT consider collisions.
    /// </summary>
    /// <param name="current">Current position, velocity, and time</param>
    /// <param name="targetTime">Time to predict</param>
    /// <returns>Predicted position and velocity at the given time</returns>
    private static PlayerPhysicsFrame RawDeadReckonDeg2(PlayerPhysicsFrame current, float targetTime)
    {
        float dt = targetTime - current.time;

        if (dt < 0) Debug.LogWarning("Rewinding time is strongly discouraged! (dt="+dt+")");
        
        current.position += current.velocity*dt + 1/2*Physics.gravity*dt*dt; // s = ut + 1/2 at^2
        current.velocity += Physics.gravity*dt; // v = u + at
        current.time = targetTime;

        return current;
    }

    /// <summary>
    /// 3rd degree dead reckoning. Considers velocity, gravity, and input. Does NOT consider collisions.
    /// </summary>
    /// <param name="current">Current position, velocity, and time</param>
    /// <param name="targetTime">Time to predict</param>
    /// <returns>Predicted position and velocity at the given time</returns>
    private static PlayerPhysicsFrame RawDeadReckonDeg3(PlayerPhysicsFrame current, float targetTime)
    {
        float dt = targetTime - current.time;

        //if (dt < 0) throw new InvalidOperationException("Rewinding time is not allowed due to discontinuity! (dt="+dt+")");
        if (dt < 0)
        {
            Debug.LogWarning("Negative DT! Deferring to 2nd degree.");
            return RawDeadReckonDeg2(current, targetTime);
        }

        Vector3 targetVelocity = current.LookRight * current.input.x + current.LookForward * current.input.y;
        targetVelocity.y = current.velocity.y;
        float px = Mathf.Pow(current.slipperiness, dt);

        current.position += targetVelocity*dt+(current.velocity-targetVelocity)*(px-1)/Mathf.Log(current.slipperiness) + 1/2*Physics.gravity*dt*dt;
        current.velocity = Vector3.Lerp(current.velocity, targetVelocity, px) + Physics.gravity*dt; //NOTE: This will 100% break if gravity ever goes sideways
        current.time = targetTime;

        return current;
    }
}
