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
    [SerializeField]
    Button lobbyStartGameButton;

    float lobbyPlayerBarHeight;
    Dictionary<string, GameObject> playerIdToLobbyBar;

    private void OnEnable()
    {
        lobbyPlayerBarHeight = lobbyPlayerBarPrefab.GetComponent<RectTransform>().sizeDelta.y;

        roomCodeEntryField.onSubmit.AddListener(OnSubmitRoomCode);
        LobbyManager.Instance.onPlayersChanged += HandlePlayerChange;
        LobbyManager.Instance.onLobbyShutdown += HandleLobbyShutdown;
    }

    private void OnDisable()
    {
        roomCodeEntryField.onSubmit.RemoveListener(OnSubmitRoomCode);
        LobbyManager.Instance.onPlayersChanged -= HandlePlayerChange;
        LobbyManager.Instance.onLobbyShutdown -= HandleLobbyShutdown;
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
        lobbyRoomCodeDataDisplay.text = LobbyManager.Instance.GetLobbyCode();
        lobbyNameDataDisplay.text = LobbyManager.Instance.GetLobbyName();
        UpdateLobbySize();

        //Set up display for player data
        playerIdToLobbyBar = new Dictionary<string, GameObject>();
        foreach (Player p in LobbyManager.Instance.GetLobbyPlayers())
        {
            AddPlayerBarToLobby(p);
        }

        lobbyStartGameButton.interactable = LobbyManager.Instance.GetIsHost();
    }

    public void GoBackRoomCode()
    {
        roomCodeEntryField.text = "";
        eventSystem.SetSelectedGameObject(joinButton.gameObject);
        ToInitialChoices();
    }

    public async void HostButton()
    {
        await GameManager.Instance.BecomeHost();

        await LobbyManager.Instance.UpdateLocalPlayer();

        ToLobbyPanel();
    }

    public void StartMatchButton()
    {
        if(playerIdToLobbyBar.Count > 1)
        {
            GameManager.Instance.AttemptStartMatch();
        }
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
            await LobbyManager.Instance.UpdateLocalPlayer();

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
        if(playerIdToLobbyBar.ContainsKey(player.Id))
        {
            return;
        }

        GameObject playerBarObj = Instantiate(lobbyPlayerBarPrefab,lobbyPlayerScrollContent.transform);

        playerIdToLobbyBar.Add(player.Id, playerBarObj);

        LobbyPlayerBarController lobbyPlayerBarController = playerBarObj.GetComponent<LobbyPlayerBarController>();
        lobbyPlayerBarController.playerNameText.text = player.Data[PlayerAuthenticationManager.PLAYER_NAME_KEY].Value;

        lobbyPlayerScrollContent.sizeDelta = new Vector2(lobbyPlayerScrollContent.sizeDelta.x, lobbyPlayerBarHeight * lobbyPlayerScrollContent.transform.childCount);
    }

    private void RemovePlayerBarFromLobby(Player player)
    {
        RemovePlayerBarFromLobby(player.Id);
    }

    private void RemovePlayerBarFromLobby(string id)
    {
        Destroy(playerIdToLobbyBar[id]);
        playerIdToLobbyBar.Remove(id);
    }

    private void HandlePlayerChange()
    {
        UpdateLobbySize();
        List<Player> players = LobbyManager.Instance.GetLobbyPlayers();
        List<string> toDelete = new List<string>();

        if(playerIdToLobbyBar == null)
        {
            playerIdToLobbyBar = new Dictionary<string, GameObject>();
        }

        foreach (KeyValuePair<string, GameObject> entry in playerIdToLobbyBar)
        {
            toDelete.Add(entry.Key);
        }

        foreach (Player p in players)
        {
            if (toDelete.Contains(p.Id))
            {
                toDelete.Remove(p.Id);
            }
            else
            { 
                AddPlayerBarToLobby(p);
            }
        }

        foreach(string id in toDelete)
        {
            RemovePlayerBarFromLobby(id);
        }
    }

    private void HandleLobbyShutdown()
    {
        ClearOutPlayerBars();
        ToInitialChoices();

        //TODO: Add a pop up that informs the player why that just happened
    }

    public void LeaveLobbyButton()
    {
        LobbyManager.Instance.DisconnectFromLobby();
        ClearOutPlayerBars();
        ToInitialChoices();
    }

    private void ClearOutPlayerBars()
    {
        List<string> toDelete = new List<string>();

        foreach (KeyValuePair<string, GameObject> entry in playerIdToLobbyBar)
        {
            toDelete.Add(entry.Key);
        }

        foreach (string id in toDelete)
        {
            RemovePlayerBarFromLobby(id);
        }
    }

    void UpdateLobbySize()
    {
        lobbySizeDataDisplay.text = LobbyManager.Instance.GetLobbySize();
    }
}
