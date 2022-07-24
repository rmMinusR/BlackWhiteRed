using System;
using UnityEngine;

[Serializable]
public sealed class BoxProjection : ProjectionShape
{
    public Vector3 center;
    public Vector3 size;
    public Vector3 halfSize;

    public BoxProjection(BoxCollider source, Vector3 parentCenter) : this(source.center + source.transform.position-parentCenter, source.size) { }

    public BoxProjection(Vector3 center, Vector3 size)
    {
        this.center = center;
        this.size = size;
        halfSize = size / 2;
    }

    public override bool        Check        (                    Vector3 pos,                                         int layerMask) => Physics.CheckBox  (pos + center, halfSize, Quaternion.identity, layerMask);
    public override Collider[]  Overlap      (                    Vector3 pos,                                         int layerMask) => Physics.OverlapBox(pos + center, halfSize, Quaternion.identity, layerMask);
    public override bool        Shapecast    (out RaycastHit hit, Vector3 start, Vector3 direction, float maxDistance, int layerMask) => Physics.BoxCast   (start+center, halfSize, direction, out hit, Quaternion.identity, maxDistance, layerMask);
    public override RaycastHit[] ShapecastAll(                    Vector3 start, Vector3 direction, float maxDistance, int layerMask) => Physics.BoxCastAll(start+center, halfSize, direction,          Quaternion.identity, maxDistance, layerMask);

    protected internal override void DrawAsGizmos(Vector3 root) => Gizmos.DrawWireCube(root + center, size);
}
