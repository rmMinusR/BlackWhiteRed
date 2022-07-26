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

    public override bool Check(Vector3 pos, Quaternion rotation)
    {
        foreach (ProjectionShape c in contents) if(c.Check(pos, rotation)) return true;
        return false;
    }

    public override Collider[] Overlap(Vector3 pos, Quaternion rotation)
    {
        List<Collider> vals = new List<Collider>();
        foreach (ProjectionShape c in contents) vals.AddRange(c.Overlap(pos, rotation));
        return vals.ToArray();
    }

    public override bool Shapecast(out RaycastHit hit, Vector3 start, Vector3 direction, Quaternion rotation, float maxDistance)
    {
        //Gather all hits
        List<RaycastHit> hits = new List<RaycastHit>();
        foreach (ProjectionShape c in contents) if(c.Shapecast(out RaycastHit h, start, direction, rotation, maxDistance)) hits.Add(h);
        
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

    public override RaycastHit[] ShapecastAll(Vector3 start, Vector3 direction, Quaternion rotation, float maxDistance)
    {
        List<RaycastHit> vals = new List<RaycastHit>();
        foreach (ProjectionShape c in contents) vals.AddRange(c.ShapecastAll(start, direction, rotation, maxDistance));
        return vals.ToArray();
    }
}
