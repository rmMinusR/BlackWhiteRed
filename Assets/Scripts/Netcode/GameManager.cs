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
    private const string ENVIRONMENT = "production";
    private Guid playerAllocationId;
    private string relayJoinCode;
    RelayHostData relayHostData;

    public static GameManager Instance;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
            return;
        }

        Debug.LogError("Game Manager Instance Already Exists");
        Destroy(this);
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
        
    }

    public async Task BecomeHost()
    {
        await LobbyManager.Instance.HostLobby();
    }

    public async Task<bool> AttemptJoinWithCode(string code)
    {
        bool success = await LobbyManager.Instance.AttemptJoinLobbyWithCode(code);

        //if (success)
        //{
        //    relayJoinCode = LobbyManager.Instance.GetLobbyRelayCode();
        //    await ClientRelay();
        //}

        return success;
    }

    public void AttemptStartMatch()
    {
        StartCoroutine(RelaySetUp());
    }

    IEnumerator RelaySetUp()
    {
        HostRelay();

        var delay = new WaitForSecondsRealtime(1.0f);

        yield return delay;

        LobbyManager.Instance.SetLobbyRelayCode(relayJoinCode);
    }

    public async void JoinStartingMatch()
    {
        relayJoinCode = LobbyManager.Instance.GetLobbyRelayCode();
        await ClientRelay();
    }

    #region relay

    public async Task HostRelay()
    {
        //The host player requests an allocation
        Allocation relayAllocation = await Relay.Instance.CreateAllocationAsync(MAX_PLAYERS);

        relayHostData = new RelayHostData();
        relayHostData.mIPv4Address = relayAllocation.RelayServer.IpV4;
        relayHostData.mPort = relayAllocation.RelayServer.Port;
        relayHostData.mAllocationID = relayAllocation.AllocationId;
        relayHostData.mAllocationIDBytes = relayAllocation.AllocationIdBytes;
        relayHostData.mConnectionData = relayAllocation.ConnectionData;
        relayHostData.mKey = relayAllocation.Key;

        relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(relayHostData.mAllocationID);
        relayHostData.mJoinCode = relayJoinCode;

        Debug.Log(relayJoinCode);
        Debug.LogError(relayJoinCode);

        //TODO: Work with transport
        //UnityTransport.SetRelayServerData();
    }

    public async Task ClientRelay()
    {
        Debug.Log(relayJoinCode);
        Debug.LogError(relayJoinCode);

        await JoinRelayWithCode(relayJoinCode);
    }

    private async Task JoinRelayWithCode(string relayJoinCode)
    {
        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);
            playerAllocationId = joinAllocation.AllocationId;
        }
        catch (RelayServiceException ex)
        {
            Debug.LogError(ex.Message + "\n" + ex.StackTrace);
        }
    }

    #endregion
}
