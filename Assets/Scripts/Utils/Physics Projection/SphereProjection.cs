using System;
using UnityEngine;

[Serializable]
public sealed class SphereProjection : ProjectionShape
{
    public Vector3 center;
    public float radius;

    //public SphereProjection(SphereCollider source, Vector3 parentCenter) : this(source.center + source.transform.position-parentCenter, source.radius) { }
    public SphereProjection(SphereCollider source, Vector3 parentCenter) : this(source.center, source.radius) { }

    public SphereProjection(Vector3 center, float radius)
    {
        this.center = center;
        this.radius = radius;
    }

    public override bool         Check       (                    Vector3 pos,                                         int layerMask) => Physics.CheckSphere  (pos + center, radius, layerMask);
    public override Collider[]   Overlap     (                    Vector3 pos,                                         int layerMask) => Physics.OverlapSphere(pos + center, radius, layerMask);
    public override bool         Shapecast   (out RaycastHit hit, Vector3 start, Vector3 direction, float maxDistance, int layerMask) => Physics.SphereCast   (start+center, radius, direction, out hit, maxDistance, layerMask);
    public override RaycastHit[] ShapecastAll(                    Vector3 start, Vector3 direction, float maxDistance, int layerMask) => Physics.SphereCastAll(start+center, radius, direction,          maxDistance, layerMask);

    protected internal override void DrawAsGizmos(Vector3 root) => Gizmos.DrawWireSphere(root + center, radius);
}
