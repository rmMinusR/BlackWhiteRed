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

public class GameManager : MonoBehaviour
{
    public const int MAX_PLAYERS = 10;

    private Dictionary<string, Coroutine> coroutines;

    //Relay
    //private const string ENVIRONMENT = "production";
    //private Guid playerAllocationId;
    //private string relayJoinCode;
    //RelayHostData relayHostData;

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
    }

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

        var delay = new WaitForSecondsRealtime(1.0f);
        yield return delay;

        RelayConnectionHost temp = (RelayConnectionHost)(RelayManager.Instance.Connection);
        Debug.Assert(temp.JoinCode != null && temp.JoinCode != "", "Join code did not load up in our allotted timeframe (1 second)");

        LobbyManager.Instance.SetLobbyRelayCode(temp.JoinCode);
    }

    public void JoinStartingMatch()
    {
        string relayJoinCode = LobbyManager.Instance.GetLobbyRelayCode();
        RelayManager.Instance.StartAsClient(relayJoinCode);
    }

    //#region relay

    //public async Task HostRelay()
    //{
    //    //The host player requests an allocation
    //    Allocation relayAllocation = await Relay.Instance.CreateAllocationAsync(MAX_PLAYERS);

    //    relayHostData = new RelayHostData();
    //    relayHostData.mIPv4Address = relayAllocation.RelayServer.IpV4;
    //    relayHostData.mPort = relayAllocation.RelayServer.Port;
    //    relayHostData.mAllocationID = relayAllocation.AllocationId;
    //    relayHostData.mAllocationIDBytes = relayAllocation.AllocationIdBytes;
    //    relayHostData.mConnectionData = relayAllocation.ConnectionData;
    //    relayHostData.mKey = relayAllocation.Key;

    //    relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(relayHostData.mAllocationID);
    //    relayHostData.mJoinCode = relayJoinCode;

    //    Debug.Log(relayJoinCode);
    //    Debug.LogError(relayJoinCode);

    //    //TODO: Work with transport
    //    //UnityTransport.SetRelayServerData();
    //}

    //public async Task ClientRelay()
    //{
    //    Debug.Log(relayJoinCode);
    //    Debug.LogError(relayJoinCode);

    //    await JoinRelayWithCode(relayJoinCode);
    //}

    //private async Task JoinRelayWithCode(string relayJoinCode)
    //{
    //    try
    //    {
    //        JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);
    //        playerAllocationId = joinAllocation.AllocationId;
    //    }
    //    catch (RelayServiceException ex)
    //    {
    //        Debug.LogError(ex.Message + "\n" + ex.StackTrace);
    //    }
    //}

    //#endregion

}
