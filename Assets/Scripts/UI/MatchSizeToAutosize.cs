using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MatchSizeToAutosize : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI autoSizer;
    TextMeshProUGUI targetSizer;

    void Start()
    {
        targetSizer = GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        targetSizer.fontSize = autoSizer.fontSize;
    }
}