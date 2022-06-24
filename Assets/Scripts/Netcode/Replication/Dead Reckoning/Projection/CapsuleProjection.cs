using System;
using UnityEngine;

[Serializable]
public sealed class CapsuleProjection : PhysicsProjection
{
    public Vector3 center;
    public float radius;
    private Vector3 halfExtents;
    public Vector3 End1 => center + halfExtents;
    public Vector3 End2 => center - halfExtents;

    private static Vector3 CapsuleDirToVector(int dir_id)
    {
        return dir_id switch
        {
            0 => new Vector3(1, 0, 0),
            1 => new Vector3(0, 1, 0),
            2 => new Vector3(0, 0, 1),
            _ => throw new NotImplementedException("Unknown direction ID "+dir_id)
        };
    }

    public CapsuleProjection(CapsuleCollider source) : this(source.center, CapsuleDirToVector(source.direction) * source.height/2, source.radius) { }

    public CapsuleProjection(Vector3 center, Vector3 halfExtents, float radius)
    {
        this.center = center;
        this.halfExtents = halfExtents;
        this.radius = radius;
    }

    public override bool Check(Vector3 pos, Quaternion rotation) => Physics.CheckCapsule(pos + rotation*End1, pos + rotation*End2, radius);

    public override Collider[] Overlap(Vector3 pos, Quaternion rotation) => Physics.OverlapCapsule(pos + rotation*End1, pos + rotation*End2, radius);

    public override bool Shapecast(out RaycastHit hit, Vector3 start, Vector3 direction, Quaternion rotation, float maxDistance)
    {
        Vector3 glob_center = rotation * center;
        return Physics.CapsuleCast(start + rotation*End1, start + rotation*End2, radius, direction, out hit, maxDistance);
    }

    public override RaycastHit[] ShapecastAll(Vector3 start, Vector3 direction, Quaternion rotation, float maxDistance)
    {
        Vector3 glob_center = rotation * center;
        return Physics.CapsuleCastAll(start + rotation*End1, start + rotation*End2, radius, direction, maxDistance);
    }
}
