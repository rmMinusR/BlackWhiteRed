using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PauseScreen : MonoBehaviour
{
    [SerializeField]
    EventSystem eventSystem;
    [Header("Pause Menu")]
    [SerializeField]
    GameObject pausePanel;
    [SerializeField]
    Button pauseToBackButton;
    [SerializeField]
    Button pauseToOptionsButton;
    [Header("Options Menu")]
    [SerializeField]
    GameObject optionsPanel;
    [SerializeField]
    GameObject optionsSelectedOnOpen;

    bool ableToPause = false;
    bool isPaused = false;

    PlayerFightingInput input;

    private void Awake()
    {
        input = new PlayerFightingInput();
        ableToPause = false;
    }

    private void OnEnable()
    {
        MatchManager.onMatchStart += HandleMatchStart;

        input.Enable();
        input.Pausing.Enable();
        input.Pausing.TogglePause.Enable();
        input.Pausing.TogglePause.started += ctx => HandleTogglePause();
    }

    private void OnDisable()
    {
        MatchManager.onMatchStart -= HandleMatchStart;

        input.Disable();
        input.Pausing.Disable();
        input.Pausing.TogglePause.Disable();
        input.Pausing.TogglePause.started -= ctx => HandleTogglePause();
    }

    private void HandleMatchStart()
    {
        ableToPause = true;
    }

    public void HandleTogglePause()
    {
        if(ableToPause)
        {
            isPaused = !isPaused;
        }

        if(isPaused)
        {
            PlayerLookController.cursorLocked = false;
            GameplayToPause();
        }
        else
        {
            PlayerLookController.cursorLocked = true;
            CloseAllWindows();
        }
    }

    public void CloseAllWindows()
    {
        pausePanel.SetActive(false);
        optionsPanel.SetActive(false);
    }

    public void OpenPause()
    {
        CloseAllWindows();
        pausePanel.SetActive(true);
    }

    public void OpenOptions()
    {
        CloseAllWindows();
        optionsPanel.SetActive(true);
        eventSystem.SetSelectedGameObject(optionsSelectedOnOpen);
    }

    public void OptionsToPause()
    {
        OpenPause();
        eventSystem.SetSelectedGameObject(pauseToOptionsButton.gameObject);
    }

    public void GameplayToPause()
    {
        OpenPause();
        eventSystem.SetSelectedGameObject(pauseToBackButton.gameObject);
    }
}
