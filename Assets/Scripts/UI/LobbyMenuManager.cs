using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using TMPro;

public class LobbyMenuManager : MonoBehaviour
{
    [SerializeField]
    EventSystem eventSystem;
    [Space]
    [Header("Initial Choices")]
    [SerializeField]
    GameObject initialChoicesPanel;
    [SerializeField]
    Button joinButton;
    [SerializeField]
    Button hostButton;
    [Space]
    [Header("Room Code")]
    [SerializeField]
    GameObject roomCodePanel;
    [SerializeField]
    TMP_InputField roomCodeEntryField;
    [Space]
    [Header("Lobby")]
    [SerializeField]
    GameObject lobbyPanel;
    [SerializeField]
    TextMeshProUGUI lobbyRoomCodeDataDisplay;
    [SerializeField]
    TextMeshProUGUI lobbySizeDataDisplay;
    [SerializeField]
    TextMeshProUGUI lobbyNameDataDisplay;
    [SerializeField]
    RectTransform lobbyPlayerScrollContent;
    [SerializeField]
    GameObject lobbyPlayerBarPrefab;

    float lobbyPlayerBarHeight;
    Dictionary<string, GameObject> playerIdToLobbyBar;

    private void OnEnable()
    {
        lobbyPlayerBarHeight = lobbyPlayerBarPrefab.GetComponent<RectTransform>().sizeDelta.y;

        roomCodeEntryField.onSubmit.AddListener(OnSubmitRoomCode);
    }

    private void OnDisable()
    {
        roomCodeEntryField.onSubmit.RemoveListener(OnSubmitRoomCode);
    }

    public void ClearPanels()
    {
        initialChoicesPanel.SetActive(false);
        roomCodePanel.SetActive(false);
        lobbyPanel.SetActive(false);
    }

    public void ToInitialChoices()
    {
        ClearPanels();
        initialChoicesPanel.SetActive(true);
    }

    public void ToRoomCode()
    {
        roomCodeEntryField.text = "";
        ClearPanels();
        roomCodePanel.SetActive(true);
        eventSystem.SetSelectedGameObject(roomCodeEntryField.gameObject);
    }

    private void ToLobbyPanel()
    {
        ClearPanels();
        lobbyPanel.SetActive(true);

        //Set up display for lobby data
        lobbyRoomCodeDataDisplay.text = GameManager.Instance.GetLobbyCode();
        lobbySizeDataDisplay.text = GameManager.Instance.GetLobbySize();
        lobbyNameDataDisplay.text = GameManager.Instance.GetLobbyName();

        //Set up display for player data
        playerIdToLobbyBar = new Dictionary<string, GameObject>();
        foreach (Player p in GameManager.Instance.GetLobbyPlayers())
        {
            AddPlayerBarToLobby(p);
        }
    }

    public void GoBackRoomCode()
    {
        roomCodeEntryField.text = "";
        eventSystem.SetSelectedGameObject(joinButton.gameObject);
        ToInitialChoices();
    }

    public async void HostButton()
    {
        await GameManager.Instance.HostLobby();

        await GameManager.Instance.UpdateLocalPlayer();

        ToLobbyPanel();
    }

    public void SubmitRoomCodeButton()
    {
        OnSubmitRoomCode(roomCodeEntryField.text);
    }

    private async void OnSubmitRoomCode(string input)
    {
        bool success = await GameManager.Instance.AttemptJoinWithCode(input);

        if (success)
        {
            await GameManager.Instance.UpdateLocalPlayer();

            lobbyRoomCodeDataDisplay.text = input;
            ToLobbyPanel();
        }
    }

    public void BackToMainMenu()
    {
        SceneManager.LoadScene(1);
    }

    private void AddPlayerBarToLobby(Player player)
    {
        GameObject playerBarObj = Instantiate(lobbyPlayerBarPrefab,lobbyPlayerScrollContent.transform);

        playerIdToLobbyBar.Add(player.Id, playerBarObj);

        LobbyPlayerBarController lobbyPlayerBarController = playerBarObj.GetComponent<LobbyPlayerBarController>();
        lobbyPlayerBarController.playerNameText.text = player.Data[GameManager.PLAYER_NAME_KEY].Value;

        lobbyPlayerScrollContent.sizeDelta = new Vector2(lobbyPlayerScrollContent.sizeDelta.x, lobbyPlayerBarHeight * lobbyPlayerScrollContent.transform.childCount);
    }

    private void RemovePlayerBarFromLobby(Player player)
    {
        Destroy(playerIdToLobbyBar[player.Id]);
        playerIdToLobbyBar.Remove(player.Id);
    }
}
