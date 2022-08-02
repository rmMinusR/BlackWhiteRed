using System.Collections;
using UnityEngine;

public sealed class BillboardTowardsMainCamera : MonoBehaviour
{
    private Transform target;

    void Update()
    {
        if (target == null) target = Camera.main.transform;

        if (target != null) transform.rotation = target.rotation;
    }
}