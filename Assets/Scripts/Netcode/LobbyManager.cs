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
    private const string LOBBY_HEARTBEAT_COROUTINE_KEY = "heartbeat coroutine";
    private bool isHost;
    private Lobby inLobby;
    private Coroutine heartbeatCoroutine;

    private string playerId => GameManager.Instance.GetPlayerID();
    private string playerName => GameManager.Instance.GetPlayerName();

    private LobbyManager()
    {
    }

    public async Task HostLobby()
    {
        string lobbyName = playerName + "'s Lobby";
        CreateLobbyOptions options = new CreateLobbyOptions();
        options.IsPrivate = true;

        inLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, GameManager.MAX_PLAYERS, options);

        GameManager.Instance.PromptCoroutine(LOBBY_HEARTBEAT_COROUTINE_KEY, HeartbeatLobbyCoroutine(inLobby.Id, 15));
    }

    IEnumerator HeartbeatLobbyCoroutine(string lobbyId, float waitTimeSeconds)
    {
        var delay = new WaitForSecondsRealtime(waitTimeSeconds);

        while (true)
        {
            LobbyService.Instance.SendHeartbeatPingAsync(lobbyId);
            yield return delay;
        }
    }

    public async Task<bool> AttemptJoinLobbyWithCode(string code)
    {
        bool success = true;

        try
        {
            inLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(code);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
            success = false;
        }

        return success;
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
                    GameManager.PLAYER_NAME_KEY, new PlayerDataObject(
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
}
