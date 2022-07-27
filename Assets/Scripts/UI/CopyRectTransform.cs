using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public sealed class CopyRectTransform : MonoBehaviour
{
    [SerializeField] private RectTransform source;
    [SerializeField] private bool copyPosition;
    [SerializeField] private bool copyWidth;
    [SerializeField] private bool copyHeight;

    void Update()
    {
        RectTransform dst = (RectTransform) transform;
        if(copyPosition) dst.position = source.position;
        if (copyWidth || copyHeight)
        {
            dst.sizeDelta = new Vector2(
                copyWidth ? source.sizeDelta.x : dst.sizeDelta.x,
                copyHeight ? source.sizeDelta.y : dst.sizeDelta.y
            );
        }
    }
}
