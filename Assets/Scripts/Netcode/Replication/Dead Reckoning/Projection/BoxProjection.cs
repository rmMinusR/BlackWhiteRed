using System;
using UnityEngine;

[Serializable]
public sealed class BoxProjection : PhysicsProjection
{
    public Vector3 center;
    public Vector3 size;
    public Vector3 halfSize;

    public BoxProjection(BoxCollider source) : this(source.center, source.size) { }

    public BoxProjection(Vector3 center, Vector3 size)
    {
        this.center = center;
        this.size = size;
        halfSize = size / 2;
    }

    public override bool Check(Vector3 pos, Quaternion rotation) => Physics.CheckBox(pos + rotation*center, halfSize, rotation);

    public override Collider[] Overlap(Vector3 pos, Quaternion rotation) => Physics.OverlapBox(pos + rotation*center, halfSize, rotation);

    public override bool Shapecast(out RaycastHit hit, Vector3 start, Vector3 direction, Quaternion rotation, float maxDistance)
    {
        Vector3 glob_center = rotation * center;
        return Physics.BoxCast(start+glob_center, halfSize, direction, out hit, rotation, maxDistance);
    }

    public override RaycastHit[] ShapecastAll(Vector3 start, Vector3 direction, Quaternion rotation, float maxDistance)
    {
        Vector3 glob_center = rotation * center;
        return Physics.BoxCastAll(start+glob_center, halfSize, direction, rotation, maxDistance);
    }
}
