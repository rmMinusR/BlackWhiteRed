using System;
using UnityEngine;

internal interface IPhysicsFrame
{
    public Vector3 position { get; set; }
    public Vector3 velocity { get; set; }
    public float time       { get; set; }
}