using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public sealed class NetStatsDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text pingDisplay;
    [SerializeField] private TMP_Text jitterDisplay;

    private void Update()
    {
        if (NetHeartbeat.IsConnected)
        {
            if (  pingDisplay)   pingDisplay.text = ((int)(1000*NetHeartbeat.Self.SmoothedRTT))+"ms";
            if (jitterDisplay) jitterDisplay.text = ((int)(1000*NetHeartbeat.Self.Jitter     ))+"ms";
        }
    }
}
