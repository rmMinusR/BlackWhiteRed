using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
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

    private void OnEnable()
    {
        lobbyPlayerBarHeight = lobbyPlayerBarPrefab.GetComponent<RectTransform>().sizeDelta.y;

        roomCodeEntryField.onSubmit.AddListener(OnSubmitRoomCode);
    }

    private void OnDisable()
    {
        roomCodeEntryField.onSubmit.RemoveListener(OnSubmitRoomCode);
    }

    public void ToInitialChoices()
    {
        initialChoicesPanel.SetActive(true);
        roomCodePanel.SetActive(false);
        lobbyPanel.SetActive(false);
    }

    public void ToRoomCode()
    {
        roomCodeEntryField.text = "";
        initialChoicesPanel.SetActive(false);
        roomCodePanel.SetActive(true);
        lobbyPanel.SetActive(false);
        eventSystem.SetSelectedGameObject(roomCodeEntryField.gameObject);
    }

    public void ToLobbyPanel()
    {
        initialChoicesPanel.SetActive(false);
        roomCodePanel.SetActive(false);
        lobbyPanel.SetActive(true);
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

        lobbyRoomCodeDataDisplay.text = GameManager.Instance.GetLobbyCode();
        lobbySizeDataDisplay.text = GameManager.Instance.GetLobbySize();
        lobbyNameDataDisplay.text = GameManager.Instance.GetLobbyName();
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
            lobbyRoomCodeDataDisplay.text = input;
            ToLobbyPanel();
        }
    }

    public void BackToMainMenu()
    {
        SceneManager.LoadScene(1);
    }

    private void AddPlayerBarToLobby()
    {
        Instantiate(lobbyPlayerBarPrefab,lobbyPlayerScrollContent.transform);
        lobbyPlayerScrollContent.sizeDelta = new Vector2(lobbyPlayerScrollContent.sizeDelta.x, lobbyPlayerBarHeight * lobbyPlayerScrollContent.transform.childCount);
    }
}
