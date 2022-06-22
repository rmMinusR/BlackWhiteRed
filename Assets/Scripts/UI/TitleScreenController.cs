using System.Collections;
using System.Collections.Generic;
using Unity.Services.Core;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class TitleScreenController : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI waitingMessage;
    [SerializeField]
    Button startButton;

    private async void OnEnable()
    {
        await UnityServices.InitializeAsync();
        AuthenticationService.Instance.SignedIn += RevealButton;
    }

    private void OnDisable()
    {
        AuthenticationService.Instance.SignedIn -= RevealButton;
    }

    private void RevealButton()
    {
        //Skip this splash screen
        ToMainMenu();

        waitingMessage.gameObject.SetActive(false);
        startButton.gameObject.SetActive(true);
    }

    public void ToMainMenu()
    {
        SceneManager.LoadScene(1);
    }
}
