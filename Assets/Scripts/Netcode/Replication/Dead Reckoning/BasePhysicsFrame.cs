using System;
using UnityEngine;

internal interface BasePhysicsFrame
{
    public Vector3 position { get; internal set; }
    public Vector3 velocity { get; internal set; }
    public float time { get; internal set; }

    public BasePhysicsFrame Clone();
}
