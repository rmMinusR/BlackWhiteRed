using System;
using UnityEngine;

[Serializable]
public sealed class CapsuleProjection : ProjectionShape
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

    //public CapsuleProjection(    CapsuleCollider source, Vector3 parentCenter) : this(source.center + source.transform.position-parentCenter, CapsuleDirToVector(source.direction) * source.height/2, source.radius) { }
    //public CapsuleProjection(CharacterController source, Vector3 parentCenter) : this(source.center + source.transform.position-parentCenter,                           Vector3.up * source.height/2, source.radius) { }

    public CapsuleProjection(    CapsuleCollider source, Vector3 parentCenter) : this(source.center, CapsuleDirToVector(source.direction) * source.height/2, source.radius) { }
    public CapsuleProjection(CharacterController source, Vector3 parentCenter) : this(source.center,                           Vector3.up * source.height/2, source.radius) { }

    public CapsuleProjection(Vector3 center, Vector3 halfExtents, float radius)
    {
        this.center = center;
        this.halfExtents = halfExtents;
        this.radius = radius;
    }

    public override bool         Check       (                    Vector3 pos,                                         int layerMask) => Physics.CheckCapsule  (  pos + End1,   pos + End2, radius, layerMask);
    public override Collider[]   Overlap     (                    Vector3 pos,                                         int layerMask) => Physics.OverlapCapsule(  pos + End1,   pos + End2, radius, layerMask);
    public override bool         Shapecast   (out RaycastHit hit, Vector3 start, Vector3 direction, float maxDistance, int layerMask) => Physics.CapsuleCast   (start + End1, start + End2, radius, direction, out hit, maxDistance, layerMask);
    public override RaycastHit[] ShapecastAll(                    Vector3 start, Vector3 direction, float maxDistance, int layerMask) => Physics.CapsuleCastAll(start + End1, start + End2, radius, direction,          maxDistance, layerMask);

    protected internal override void DrawAsGizmos(Vector3 root) => Gizmos.DrawWireMesh(PrimitiveHelper.GetPrimitiveMesh(PrimitiveType.Capsule), root + center);
}
