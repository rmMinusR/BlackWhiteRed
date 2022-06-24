using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class DeadReckoningUtility
{
    /// <summary>
    /// Alias to preferred dead reckoning method. Does NOT consider collisions.
    /// Current: 2nd degree
    /// </summary>
    /// <param name="current">Current position, velocity, and time</param>
    /// <param name="targetTime">Time to predict</param>
    /// <returns>Predicted position and velocity at the given time</returns>
    public static PhysicsFrame RawDeadReckon(PhysicsFrame current, float targetTime) => RawDeadReckonDeg2(current, targetTime);

    /// <summary>
    /// 1st degree dead reckoning. Only considers current velocity. Does NOT consider collisions.
    /// </summary>
    /// <param name="current">Current position, velocity, and time</param>
    /// <param name="targetTime">Time to predict</param>
    /// <returns>Predicted position and velocity at the given time</returns>
    public static PhysicsFrame RawDeadReckonDeg1(PhysicsFrame current, float targetTime)
    {
        float dt = targetTime - current.time;

        if (dt < 0) Debug.LogWarning("Rewinding time is strongly discouraged! (dt="+dt+")");
        
        return new PhysicsFrame()
        {
            position = current.position + current.velocity*dt, // s = ut
            velocity = current.velocity,
            time = targetTime
        };
    }

    /// <summary>
    /// 2nd degree dead reckoning. Considers both velocity and gravity. Does NOT consider collisions.
    /// </summary>
    /// <param name="current">Current position, velocity, and time</param>
    /// <param name="targetTime">Time to predict</param>
    /// <returns>Predicted position and velocity at the given time</returns>
    public static PhysicsFrame RawDeadReckonDeg2(PhysicsFrame current, float targetTime)
    {
        float dt = targetTime - current.time;

        if (dt < 0) Debug.LogWarning("Rewinding time is strongly discouraged! (dt="+dt+")");
        
        return new PhysicsFrame()
        {
            position = current.position + current.velocity*dt + 1/2*Physics.gravity*dt*dt, // s = ut + 1/2 at^2
            velocity = current.velocity + Physics.gravity*dt, // v = u + at
            time = targetTime
        };
    }

    private const int MAX_COLLISIONS = 3;
    private const int COLLISION_PRECISION = 8;

    public static PhysicsFrame DeadReckon(PhysicsFrame current, float targetTime, PhysicsProjection shape, Quaternion rotation)
    {
        int nCollisions = 0;
        do
        {
            RaycastHit? hit;
            (current, hit) = _SimulateUntilCollision(current, targetTime, shape, rotation);
            if (hit.HasValue) current = _CalcCollisionResponse(current, hit.Value);

            ++nCollisions;
        }
        while (nCollisions < MAX_COLLISIONS && current.time < targetTime);
        return current;
    }

    private static (PhysicsFrame final, RaycastHit? hit) _SimulateUntilCollision(PhysicsFrame arc, float targetTime, PhysicsProjection shape, Quaternion rotation)
    {
        PhysicsFrame searchStart = arc;
        PhysicsFrame searchEnd = RawDeadReckon(searchStart, targetTime);

        //Binary search to find time of collision (if any)
        //TODO can this be improved with Newton's approximation method?
        RaycastHit hit = default;
        for (int i = 0; i < COLLISION_PRECISION; ++i)
        {
            Vector3 dir = searchEnd.position-searchStart.position;
            float dist = dir.magnitude;

            //Abort early if nothing hit
            if (!shape.Shapecast(out hit, searchStart.position, dir, rotation, dist)) return (searchEnd, null);
            
            float guessRatio = hit.distance / dist;
            float guessTime = Mathf.Lerp(searchStart.time, searchEnd.time, guessRatio);
            if (guessRatio > 0.5f) searchEnd = RawDeadReckon(arc, guessTime);
            else                 searchStart = RawDeadReckon(arc, guessTime);
        }

        return (searchStart, hit);
    }

    private static PhysicsFrame _CalcCollisionResponse(PhysicsFrame current, RaycastHit hit)
    {
        current.velocity = Vector3.ProjectOnPlane(current.velocity, hit.normal); //TODO is this correct?
        return current;
    }
}
