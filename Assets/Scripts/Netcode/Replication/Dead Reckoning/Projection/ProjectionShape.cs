using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public abstract class ProjectionShape
{
    #region Building

    public static ProjectionShape Build(GameObject source)
    {
        List<ProjectionShape> projections = source.GetComponents<Collider>().Select(Resolve).ToList();
        if (projections.Count == 1) return projections[0];
        else if (projections.Count == 0) throw new InvalidOperationException("Can't build projections for "+source+" because it has no colliders");
        else return new CompoundProjection(projections);
    }

    private static ProjectionShape Resolve(Collider coll)
    {
        if(coll is SphereCollider s) return new SphereProjection(s);
        if(coll is CapsuleCollider c) return new CapsuleProjection(c);
        if(coll is CharacterController ch) return new CapsuleProjection(ch);
        if(coll is BoxCollider b) return new BoxProjection(b);
        
        throw new NotImplementedException("Unhandled type: " + coll.GetType());
    }

    #endregion

    #region Instance

    protected ProjectionShape() { }

    public abstract bool Check(Vector3 pos, Quaternion rotation);
    public abstract Collider[] Overlap(Vector3 pos, Quaternion rotation);
    public abstract bool Shapecast(out RaycastHit hit, Vector3 start, Vector3 direction, Quaternion rotation, float maxDistance);
    public abstract RaycastHit[] ShapecastAll(Vector3 start, Vector3 direction, Quaternion rotation, float maxDistance);

    #endregion
}
