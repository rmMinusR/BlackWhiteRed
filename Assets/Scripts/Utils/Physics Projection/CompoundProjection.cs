using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class CompoundProjection : ProjectionShape
{
    public List<ProjectionShape> contents;

    public CompoundProjection(List<ProjectionShape> source)
    {
        contents = source;
    }

    public override bool Check(Vector3 pos, int layerMask)
    {
        foreach (ProjectionShape c in contents) if(c.Check(pos, layerMask)) return true;
        return false;
    }

    public override Collider[] Overlap(Vector3 pos, int layerMask)
    {
        List<Collider> vals = new List<Collider>();
        foreach (ProjectionShape c in contents) vals.AddRange(c.Overlap(pos, layerMask));
        return vals.ToArray();
    }

    public override bool Shapecast(out RaycastHit hit, Vector3 start, Vector3 direction, float maxDistance, int layerMask)
    {
        //Gather all hits
        List<RaycastHit> hits = new List<RaycastHit>();
        foreach (ProjectionShape c in contents) if(c.Shapecast(out RaycastHit h, start, direction, maxDistance, layerMask)) hits.Add(h);
        
        if (hits.Count == 0)
        {
            //Nothing to hit
            hit = default;
            return false;
        }
        else
        {
            //Find what hit first
            hit = hits[0];
            for(int i = 1; i < hits.Count; ++i) if (hits[i].distance < hit.distance) hit = hits[i];

            return true;
        } 

    }

    public override RaycastHit[] ShapecastAll(Vector3 start, Vector3 direction, float maxDistance, int layerMask)
    {
        List<RaycastHit> vals = new List<RaycastHit>();
        foreach (ProjectionShape c in contents) vals.AddRange(c.ShapecastAll(start, direction, maxDistance, layerMask));
        return vals.ToArray();
    }

    protected internal override void DrawAsGizmos(Vector3 root)
    {
        foreach (ProjectionShape c in contents) c.DrawAsGizmos(root);
    }
}
