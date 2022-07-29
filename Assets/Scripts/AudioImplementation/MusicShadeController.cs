using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicShadeController : MonoBehaviour
{
    public FMODUnity.EventReference musicEventName;
    public FMOD.Studio.EventInstance musicEvent;

    [Space]

    [SerializeField]
    [Tooltip("Parameter name for the shade/layer value")]
    string param;
    [SerializeField]
    int lastSetValue;

    [SerializeField]
    PlayerController localPlayer;

    void Start()
    {
        musicEvent = FMODUnity.RuntimeManager.CreateInstance(musicEventName);
    }

    private void OnEnable()
    {
        MatchManager.onMatchStart += HandleMatchStart;
        ListenToPlayer();
    }

    private void OnDisable()
    {
        MatchManager.onMatchStart -= HandleMatchStart;
        StopListeningToPlayer();
    }

    private void HandleShadeChange(PlayerStats _value)
    {
        musicEvent.setParameterByName(param, localPlayer.ShadeValue);
        lastSetValue = localPlayer.ShadeValue;
    }

    private void ListenToPlayer()
    {
        if (localPlayer != null)
        {
            localPlayer.onShadeChange += HandleShadeChange;
        }
    }

    private void StopListeningToPlayer()
    {
        if(localPlayer != null)
        {
            localPlayer.onShadeChange -= HandleShadeChange;
        }
    }

    private void HandleMatchStart()
    {
        localPlayer = MatchManager.Instance.localPlayerController;

        musicEvent.setParameterByName(param, localPlayer.ShadeValue);
        lastSetValue = localPlayer.ShadeValue;

        musicEvent.start();

        ListenToPlayer();
    }

    private void OnDestroy()
    {
        musicEvent.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
    }
}
