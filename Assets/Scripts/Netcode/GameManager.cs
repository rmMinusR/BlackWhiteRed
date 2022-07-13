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

public class GameManager : MonoBehaviour
{
    public const int MAX_PLAYERS = 10;

    private Dictionary<string, Coroutine> coroutines;

    const string SceneNamePlayers = "Level3-Area0-Players";
    const string SceneNameLevelDesign = "Level3-Area0-LevelDesign";
    const string SceneNameEnvironmentArt = "Level3-Area0-EnvironmentArt";

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
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= HandleSceneLoadEventCompleted;
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

    }

    IEnumerator WaitForClientConnection()
    {
        RelayConnectionClient temp = (RelayConnectionClient)RelayManager.Instance.Connection;

        while (temp.GetStatus() != BaseRelayConnection.Status.RelayGood && temp.GetStatus() != BaseRelayConnection.Status.NGOGood)
        {
            yield return null;
            temp = (RelayConnectionClient)RelayManager.Instance.Connection;
        }

        NetworkManager.Singleton.SceneManager.SetClientSynchronizationMode(LoadSceneMode.Single);
        NetworkManager.Singleton.SceneManager.OnLoadComplete += HandleSceneLoadCompleted;
    }

    #endregion

    #region game start

    private void WhenAllPlayersConnected()
    {
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += HandleSceneLoadEventCompleted;
        NetworkManager.Singleton.SceneManager.SetClientSynchronizationMode(LoadSceneMode.Single);
        NetworkManager.Singleton.SceneManager.LoadScene(SceneNamePlayers,LoadSceneMode.Single);
    }

    private void HandleSceneLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (NetworkManager.Singleton.IsHost)
        {
            switch (sceneName)
            {
                case SceneNamePlayers:
                    NetworkManager.Singleton.SceneManager.SetClientSynchronizationMode(LoadSceneMode.Additive);
                    NetworkManager.Singleton.SceneManager.LoadScene(SceneNameLevelDesign, LoadSceneMode.Additive);
                    break;
                case SceneNameLevelDesign:
                    NetworkManager.Singleton.SceneManager.SetClientSynchronizationMode(LoadSceneMode.Additive);
                    NetworkManager.Singleton.SceneManager.LoadScene(SceneNameEnvironmentArt, LoadSceneMode.Additive);
                    break;
                case SceneNameEnvironmentArt:
                    StartCoroutine(ReadyUp());
                    break;
            }
        }
    }

    private void HandleSceneLoadCompleted(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        if (!NetworkManager.Singleton.IsHost)
        {
            switch (sceneName)
            {
                case SceneNamePlayers:
                case SceneNameLevelDesign:
                    NetworkManager.Singleton.SceneManager.SetClientSynchronizationMode(LoadSceneMode.Additive);
                    break;
                case SceneNameEnvironmentArt:
                    AsyncOperation oper = SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene());
                    oper.completed += HandleUnloadEnded;
                    break;
            }
        }
    }

    private void HandleUnloadEnded(AsyncOperation oper)
    {
        StartCoroutine(ReadyUp());
        oper.completed -= HandleUnloadEnded;
    }

    IEnumerator ReadyUp()
    {
        while(NetworkManager.Singleton.LocalClient.PlayerObject == null)
        {
            var delay = new WaitForSecondsRealtime(0.2f);
            yield return delay;
        }

        Debug.Log("MATCH CAN START");
        MatchManager.Instance.LogAsReadyServerRpc(NetworkManager.Singleton.LocalClientId);
    }

    #endregion
}
