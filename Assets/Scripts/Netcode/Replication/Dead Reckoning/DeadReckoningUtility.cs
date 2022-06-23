using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class DeadReckoningUtility
{
    /// <summary>
    /// Alias to preferred dead reckoning method.
    /// Current: 2nd degree
    /// </summary>
    /// <param name="current">Current position, velocity, and time</param>
    /// <param name="targetTime">Time to predict</param>
    /// <returns>Predicted position and velocity at the given time</returns>
    public static PhysicsFrame DeadReckon(PhysicsFrame current, float targetTime) => DeadReckonDeg2(current, targetTime);
    
    /// <summary>
    /// 1st degree dead reckoning. Only considers current velocity.
    /// </summary>
    /// <param name="current">Current position, velocity, and time</param>
    /// <param name="targetTime">Time to predict</param>
    /// <returns>Predicted position and velocity at the given time</returns>
    public static PhysicsFrame DeadReckonDeg1(PhysicsFrame current, float targetTime)
    {
        float dt = targetTime - current.time;

        return new PhysicsFrame()
        {
            position = current.position + current.velocity*dt, // s = ut
            velocity = current.velocity,
            time = targetTime
        };
    }

    /// <summary>
    /// 2nd degree dead reckoning. Considers both velocity and gravity.
    /// </summary>
    /// <param name="current">Current position, velocity, and time</param>
    /// <param name="targetTime">Time to predict</param>
    /// <returns>Predicted position and velocity at the given time</returns>
    public static PhysicsFrame DeadReckonDeg2(PhysicsFrame current, float targetTime)
    {
        float dt = targetTime - current.time;
        
        return new PhysicsFrame()
        {
            position = current.position + current.velocity*dt + 1/2*Physics.gravity*dt*dt, // s = ut + 1/2 at^2
            velocity = current.velocity + Physics.gravity*dt, // v = u + at
            time = targetTime
        };
    }
}
