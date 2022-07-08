using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

internal static class DeadReckoningCollisionHandler<TFrame> where TFrame : struct, IPhysicsFrame
{
    /// <summary>
    /// Function for dead reckoning, without considering collisions.
    /// Predicts where `frame` will be at the target time, assuming no collisions
    /// </summary>
    /// <param name="frame">Current frame (input) and output frame</param>
    /// <param name="targetTime">Time to predict</param>
    public delegate TFrame RawPredictFunc(TFrame frame, float targetTime);

    /// <summary>
    /// Dead reckon using preferred method. Considers collisions.
    /// </summary>
    /// <param name="frame">Current frame (input) and output frame</param>
    /// <param name="targetTime">Time to simulate until</param>
    /// <param name="shape">Representation of colliders. See ProjectionShape.Build</param>
    /// <param name="rotation"></param>
    /// <returns>Predicted position and velocity at the given time</returns>
    public static TFrame DeadReckon(TFrame frame, float targetTime, ProjectionShape shape, Quaternion rotation, RawPredictFunc reckon, int maxCollisions = 1, int collisionPrecision = 4)
    {
        int nCollisions = 0;
        do
        {
            RaycastHit? hit;
            (frame, hit) = _SimulateUntilCollision(frame, targetTime, shape, rotation, reckon, collisionPrecision);
            
            if (hit.HasValue) frame = _CalcCollisionResponse(frame, hit.Value);

            ++nCollisions;
        }
        while (nCollisions < maxCollisions && frame.time < targetTime);
        return frame;
    }

    private static (TFrame final, RaycastHit? hit) _SimulateUntilCollision(TFrame arc, float targetTime, ProjectionShape shape, Quaternion rotation, RawPredictFunc reckon, int collisionPrecision)
    {
        TFrame searchStart = arc;
        TFrame searchEnd = reckon(searchStart, targetTime);

        //TODO can this be improved with Newton's approximation method?

        /*
        //Pure binary search to find time of collision (if any)
        RaycastHit hit = default;
        for (int i = 0; i < collisionPrecision; ++i)
        {
            Vector3 dir = searchEnd.position-searchStart.position;
            float dist = dir.magnitude;

            //Abort early if nothing hit
            if (!shape.Shapecast(out hit, searchStart.position, dir, rotation, dist)) return (searchEnd, null);

            float guessRatio = hit.distance / dist;
            float guessTime = (searchStart.time+searchEnd.time)/2;
            if (guessRatio > 0.5f) searchEnd   = reckon(arc, guessTime);
            else                   searchStart = reckon(arc, guessTime);
        }
        // */

        //*
        //Guesstimate binary search to find time of collision (if any)
        RaycastHit hit = default;
        for (int i = 0; i < collisionPrecision; ++i)
        {
            Vector3 dir = searchEnd.position-searchStart.position;
            float dist = dir.magnitude;

            //Abort early if nothing hit
            if (!shape.Shapecast(out hit, searchStart.position, dir, rotation, dist)) return (searchEnd, null);

            float guessRatio = hit.distance / dist;
            guessRatio = Mathf.Clamp(guessRatio, 0.25f, 0.75f); //Prevent guess from approaching extremes and breaking the binary search
            float guessTime = Mathf.Lerp(searchStart.time, searchEnd.time, guessRatio);
            if (guessRatio > 0.5f) searchEnd   = reckon(arc, guessTime);
            else                   searchStart = reckon(arc, guessTime);
        }
        // */

        return (searchStart, hit);
    }

    private static TFrame _CalcCollisionResponse(TFrame current, RaycastHit hit)
    {
        current.velocity = Vector3.ProjectOnPlane(current.velocity, hit.normal); //TODO is this correct?
        return current;
    }
}
