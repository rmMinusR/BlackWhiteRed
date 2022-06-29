using System;
using UnityEngine;

[Serializable]
public sealed class SphereProjection : ProjectionShape
{
    public Vector3 center;
    public float radius;

    public SphereProjection(SphereCollider source) : this(source.center, source.radius) { }

    public SphereProjection(Vector3 center, float radius)
    {
        this.center = center;
        this.radius = radius;
    }

    public override bool Check(Vector3 pos, Quaternion rotation) => Physics.CheckSphere(pos + rotation*center, radius);

    public override Collider[] Overlap(Vector3 pos, Quaternion rotation) => Physics.OverlapSphere(pos + rotation*center, radius);

    public override bool Shapecast(out RaycastHit hit, Vector3 start, Vector3 direction, Quaternion rotation, float maxDistance)
    {
        Vector3 glob_center = rotation * center;
        return Physics.SphereCast(start+glob_center, radius, direction, out hit, maxDistance);
    }

    public override RaycastHit[] ShapecastAll(Vector3 start, Vector3 direction, Quaternion rotation, float maxDistance)
    {
        Vector3 glob_center = rotation * center;
        return Physics.SphereCastAll(start+glob_center, radius, direction, maxDistance);
    }
}
