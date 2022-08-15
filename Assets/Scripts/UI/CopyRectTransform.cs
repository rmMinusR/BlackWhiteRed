using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public sealed class CopyRectTransform : MonoBehaviour
{
    [SerializeField] private RectTransform source;
    [SerializeField] private bool copyPosition;
    [SerializeField] private bool copyWidth;
    [SerializeField] private bool copyHeight;

    private LayoutElement layoutElement; //Optional
    private void Start()
    {
        layoutElement = GetComponent<LayoutElement>();
    }

    void Update()
    {
        if(layoutElement == null)
        {
            //Overwrite transform
            RectTransform dst = (RectTransform) transform;
            if(copyPosition) dst.position = source.position;
            if (copyWidth || copyHeight)
            {
                dst.sizeDelta = new Vector2(
                    copyWidth  ? source.sizeDelta.x : dst.sizeDelta.x,
                    copyHeight ? source.sizeDelta.y : dst.sizeDelta.y
                );
            }
        }
        else
        {
            LayoutRebuilder.MarkLayoutForRebuild((RectTransform) transform);

            //Write to LayoutElement values and let parent Layout do the rest
            if (copyWidth ) layoutElement.preferredWidth  = source.sizeDelta.x;
            if (copyHeight) layoutElement.preferredHeight = source.sizeDelta.y;
        }

    }
}
