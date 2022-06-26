using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ShadeData : MonoBehaviour
{
    [SerializeField]
    [Range(0, 6)]
    private int shadeValue;

    public int ShadeValue => shadeValue;

    private void OnDrawGizmos()
    {
        float portion = shadeValue / 6.0f;
        Gizmos.color = new Color(portion, portion, portion, 0.75f);

        BoxCollider boxCollider;
        if (TryGetComponent<BoxCollider>(out boxCollider))
        {
            Gizmos.DrawCube(boxCollider.center + transform.position, boxCollider.size);
        }
    }
}
