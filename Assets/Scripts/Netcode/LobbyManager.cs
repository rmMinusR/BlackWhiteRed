using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Threading.Tasks;
using System;


public class LobbyManager
{
    private static LobbyManager instance;
    public static LobbyManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new LobbyManager();

            }
            return instance;
        }
    }

    private const string LOBBY_RELAY_CODE_KEY = "relay code";
    private const string LOBBY_GAME_START_KEY = "game started";
    private const string LOBBY_HEARTBEAT_COROUTINE_KEY = "heartbeat coroutine";
    private const string LOBBY_POLL_COROUTINE_KEY = "poll lobby coroutine";
    private bool isHost;
    private Lobby inLobby;

    //Events
    public delegate void TriggerEvent();
    public event TriggerEvent onPlayersChanged;
    public event TriggerEvent onGameStartChanged;
    public event TriggerEvent onLobbyShutdown;

    private string playerId => PlayerAuthenticationManager.Instance.GetPlayerID();
    private string playerName => PlayerAuthenticationManager.Instance.GetPlayerName();

    private LobbyManager()
    {
        isHost = false;
        inLobby = null;
    }

    public void HandleApplicationClose()
    {
        DisconnectFromLobby();
    }

    public async Task HostLobby()
    {
        string lobbyName = playerName + "'s Lobby";
        CreateLobbyOptions options = new CreateLobbyOptions();
        options.IsPrivate = true;
        options.Data = new Dictionary<string, DataObject>()
            {
                {
                    LOBBY_GAME_START_KEY, new DataObject(
                        visibility: DataObject.VisibilityOptions.Public,
                        value: "FALSE")
                }
            };

        inLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, GameManager.MAX_PLAYERS, options);

        isHost = true;

        GameManager.Instance.PromptCoroutine(LOBBY_HEARTBEAT_COROUTINE_KEY, HeartbeatLobbyCoroutine(inLobby.Id, 15));
        GameManager.Instance.PromptCoroutine(LOBBY_POLL_COROUTINE_KEY, PollLobbyCoroutine(3.0f));
    }

    IEnumerator HeartbeatLobbyCoroutine(string lobbyId, float waitTimeSeconds)
    {
        var delay = new WaitForSecondsRealtime(waitTimeSeconds);

        while (true)
        {
            yield return delay;
            LobbyService.Instance.SendHeartbeatPingAsync(lobbyId);
        }
    }

    IEnumerator PollLobbyCoroutine(float waitTimeSeconds)
    {
        var delay = new WaitForSecondsRealtime(waitTimeSeconds);

        while (true)
        {
            yield return delay;
            LobbyManager.Instance.PollForLobbyUpdates();
        }
    }

    public async void PollForLobbyUpdates()
    {
        Lobby oldInLobby = inLobby;

        try
        {
            if (inLobby != null)
            {
                inLobby = await LobbyService.Instance.GetLobbyAsync(inLobby.Id);
            }
        }
        catch(LobbyServiceException e)
        {
            if (e.Reason == LobbyExceptionReason.LobbyNotFound)
            {
                inLobby = null;
                onLobbyShutdown?.Invoke();
                GameManager.Instance.PromptEndCoroutine(LOBBY_POLL_COROUTINE_KEY);
                return;
            }
        }

        //Has the game started?
        if(inLobby.Data[LOBBY_GAME_START_KEY].Value != oldInLobby.Data[LOBBY_GAME_START_KEY].Value)
        {
            if(inLobby.Data[LOBBY_GAME_START_KEY].Value == "TRUE" && !isHost)
            {
                onGameStartChanged?.Invoke();
            }
        }

        if(inLobby.Players != oldInLobby.Players)
        {
            onPlayersChanged?.Invoke();
        }
    }

    public async Task<bool> AttemptJoinLobbyWithCode(string code)
    {
        bool success = true;

        try
        {
            inLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(code);
            GameManager.Instance.PromptCoroutine(LOBBY_POLL_COROUTINE_KEY, PollLobbyCoroutine(3.0f));
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
            success = false;
        }

        return success;
    }

    public async void DisconnectFromLobby()
    {
        if (isHost)
        {
            await LobbyService.Instance.DeleteLobbyAsync(inLobby.Id);
        GameManager.Instance.PromptEndCoroutine(LOBBY_HEARTBEAT_COROUTINE_KEY);
        }
        else
        {
            await LobbyService.Instance.RemovePlayerAsync(inLobby.Id, playerId);
        }

        GameManager.Instance.PromptEndCoroutine(LOBBY_POLL_COROUTINE_KEY);

        inLobby = null;
    }

    public string GetLobbyCode()
    {
        return inLobby.LobbyCode;
    }

    public string GetLobbySize()
    {
        return (10 - inLobby.AvailableSlots) + "/10";
    }

    public string GetLobbyName()
    {
        return inLobby.Name;
    }

    public string GetLobbyId()
    {
        return inLobby.Id;
    }

    public List<Player> GetLobbyPlayers()
    {
        return inLobby.Players;
    }

    public async Task UpdateLocalPlayer()
    {
        try
        {
            UpdatePlayerOptions options = new UpdatePlayerOptions();

            options.Data = new Dictionary<string, PlayerDataObject>()
            {
                {
                    PlayerAuthenticationManager.PLAYER_NAME_KEY, new PlayerDataObject(
                        visibility: PlayerDataObject.VisibilityOptions.Public,
                        value: playerName)
                }
            };

            inLobby = await LobbyService.Instance.UpdatePlayerAsync(inLobby.Id, playerId, options);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    public async Task SetLobbyRelayCode(string relayJoinCode)
    {
        try
        {
            UpdateLobbyOptions options = new UpdateLobbyOptions();
            options.Name = inLobby.Name;
            options.MaxPlayers = inLobby.MaxPlayers;
            options.IsPrivate = inLobby.IsPrivate;

            options.HostId = playerId;

            options.Data = new Dictionary<string, DataObject>()
            {
                {
                    LOBBY_GAME_START_KEY, new DataObject(
                        visibility: DataObject.VisibilityOptions.Public,
                        value: "TRUE")
                },
                {
                    LOBBY_RELAY_CODE_KEY, new DataObject(
                        visibility: DataObject.VisibilityOptions.Member,
                        value: relayJoinCode)
                }
            };

            inLobby = await LobbyService.Instance.UpdateLobbyAsync(inLobby.Id, options);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    public string GetLobbyRelayCode()
    {
        return inLobby.Data[LOBBY_RELAY_CODE_KEY].Value;
    }

    public bool GetIsHost()
    {
        return isHost;
    }

    public bool GetIsInLobby()
    {
        return inLobby != null;
    }

}
