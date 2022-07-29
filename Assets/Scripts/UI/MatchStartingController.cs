using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchStartingController : MonoBehaviour
{
    [SerializeField]
    GameObject panelDisplayed;

    private void Awake()
    {
        panelDisplayed.SetActive(true);
    }

    private void OnEnable()
    {
        MatchManager.onMatchStart += HandleMatchStart;
    }

    private void OnDisable()
    {    
        MatchManager.onMatchStart -= HandleMatchStart;
    }

    private void HandleMatchStart()
    {
        panelDisplayed.SetActive(false);
    }
}
