using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MakeMaterialUnique : MonoBehaviour
{
    SkinnedMeshRenderer skinnedMeshRenderer;
    MeshRenderer meshRenderer;

    void Awake()
    {
        if (TryGetComponent<MeshRenderer>(out meshRenderer))
        {
            for (int i = 0; i < meshRenderer.materials.Length; i++)
            {
                meshRenderer.materials[i] = new Material(meshRenderer.materials[i]);
            }
        }
        else
        {
            skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
            for (int i = 0; i < skinnedMeshRenderer.materials.Length; i++)
            {
                skinnedMeshRenderer.materials[i] = new Material(skinnedMeshRenderer.materials[i]);
            }
        }
    }
}
