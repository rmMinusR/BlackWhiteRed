using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using System.Threading.Tasks;
using System;
using Unity.Netcode.Transports.UTP;
using Unity.Netcode;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public const int MAX_PLAYERS = 10;

    private Dictionary<string, Coroutine> coroutines;

    public static GameManager Instance;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);

            Application.wantsToQuit += OnWantToQuit;

            return;
        }

        Debug.LogError("Game Manager Instance Already Exists");
        Destroy(this);
    }

    private void OnDestroy()
    {
        SeverConnections();
    }

    private bool OnWantToQuit()
    {
        bool canQuit = !LobbyManager.Instance.GetIsInLobby();

        StartCoroutine(SeverConnectionsBeforeQuiting());

        return canQuit;
    }

    IEnumerator SeverConnectionsBeforeQuiting()
    {
        SeverConnections();
        yield return null;
        Application.Quit();
    }

    void SeverConnections()
    {
        LobbyManager.Instance.DisconnectFromLobby();
    }

    public void PromptCoroutine(string key, IEnumerator routine)
    {
        Coroutine value = StartCoroutine(routine);
        if(coroutines == null)
        {
            coroutines = new Dictionary<string, Coroutine>();
        }
        else if (coroutines.ContainsKey(key))
        {
            PromptEndCoroutine(key);
        }
        coroutines.Add(key, value);
    }

    public void PromptEndCoroutine(string key)
    {
        if(coroutines.ContainsKey(key))
        {
            StopCoroutine(coroutines[key]);
            coroutines.Remove(key);
        }
    }

    private async void Start()
    {
        //Fix deferred messages not reaching their targets when loading scenes
        GetComponent<NetworkManager>().NetworkConfig.SpawnTimeout = 10;

        await UnityServices.InitializeAsync();
        await PlayerAuthenticationManager.Instance.AttemptSignIn();

        PlayerAuthenticationManager.Instance.CheckForPrefs();
    }

    private void OnEnable()
    {
        LobbyManager.Instance.onGameStartChanged += JoinStartingMatch;
    }

    private void OnDisable()
    {
        LobbyManager.Instance.onGameStartChanged -= JoinStartingMatch;
    }

    #region lobby relay

    public async Task BecomeHost()
    {
        await LobbyManager.Instance.HostLobby();
    }

    public async Task<bool> AttemptJoinWithCode(string code)
    {
        bool success = await LobbyManager.Instance.AttemptJoinLobbyWithCode(code);

        return success;
    }

    public void AttemptStartMatch()
    {
        StartCoroutine(RelaySetUp());
    }

    IEnumerator RelaySetUp()
    {
        RelayManager.Instance.StartAsHost();

        RelayConnectionHost temp = (RelayConnectionHost)RelayManager.Instance.Connection;

        while (temp.GetStatus() != BaseRelayConnection.Status.RelayGood && temp.GetStatus() != BaseRelayConnection.Status.NGOGood)
        {
            var delay = new WaitForSecondsRealtime(1.0f);
            yield return delay;
            temp = (RelayConnectionHost)RelayManager.Instance.Connection;
        }

        LobbyManager.Instance.SetLobbyRelayCode(temp.JoinCode);

        while (NetworkManager.Singleton.ConnectedClientsList.Count != LobbyManager.Instance.GetNumberPlayers() - 1)
        {
            var delay = new WaitForSecondsRealtime(1.0f);
            yield return delay;
        }

        WhenAllPlayersConnected();
    }

    public void JoinStartingMatch()
    {
        string relayJoinCode = LobbyManager.Instance.GetLobbyRelayCode();
        RelayManager.Instance.StartAsClient(relayJoinCode);

        StartCoroutine(WaitForClientConnection());
        //Clientside
        //FIXME kludge
        //FindObjectOfType<MatchBeginHelper>().BeginMatch();
        LoadMatch(true, false);

    }

    IEnumerator WaitForClientConnection()
    {
        RelayConnectionClient temp = (RelayConnectionClient)RelayManager.Instance.Connection;

        while (temp.GetStatus() != BaseRelayConnection.Status.RelayGood && temp.GetStatus() != BaseRelayConnection.Status.NGOGood)
        {
            yield return null;
            temp = (RelayConnectionClient)RelayManager.Instance.Connection;
        }
    }

    #endregion

    #region game start

    private void WhenAllPlayersConnected()
    {
        //Serverside
        Debug.Assert(!NetworkManager.Singleton.NetworkConfig.EnableSceneManagement);
        //FindObjectOfType<MatchBeginHelper>().BeginMatch();
        LoadMatch(true, true); //FIXME Correct for dedicated server?
    }

    [SerializeField] private SceneLoadMonitor loadOverlay;

    const string SceneNamePlayers = "Level3-Area0-Players";
    const string SceneNameLevelDesign = "Level3-Area0-LevelDesign";
    const string SceneNameEnvironmentArt = "Level3-Area0-EnvironmentArt";

    public void LoadMatch(bool client, bool server)
    {
        SceneGroupLoader.LoadOp progress = SceneGroupLoader.Instance.LoadSceneGroupAsync(SceneNamePlayers, SceneNameLevelDesign, SceneNameEnvironmentArt);

        if (client) progress.onComplete += () => FindObjectOfType<MatchBeginHelper>().ReportLoadComplete(); //MatchManager.Instance.ReportLoadComplete();
        if (server) progress.onComplete += () => MatchManager.Instance.StartWhenPlayersLoaded();

        //Send progress monitor to UI
        if (loadOverlay != null) loadOverlay.Monitor(progress);
    }

    #endregion
}
