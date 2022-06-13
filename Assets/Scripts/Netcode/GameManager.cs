using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using System.Threading.Tasks;
using Unity.Services.Lobbies.Models;

public class GameManager : MonoBehaviour
{
    //Player Customization
    private const string PLAYER_NAME_KEY = "PLAYERNAME";
    private string playerName = "Shade";

    //Authentication
    private string playerId = "Not signed in";
    private string accessToken = "No access token";

    //Lobbies
    private Lobby hostedLobby;
    private Coroutine heartbeatCoroutine;

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

    // Start is called before the first frame update
    async void Start()
    {
        await UnityServices.InitializeAsync();
        await AttemptSignIn();

        Debug.Log(playerId);
        Debug.Log(accessToken);

        CheckForPrefs();
    }

    async Task AttemptSignIn()
    {
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        playerId = AuthenticationService.Instance.PlayerId;
        accessToken = AuthenticationService.Instance.AccessToken;
    }

    //TODO: move to some other script or manager for the player character customization
    void CheckForPrefs()
    {
        if (PlayerPrefs.HasKey(PLAYER_NAME_KEY))
        {
            playerName = PlayerPrefs.GetString(PLAYER_NAME_KEY);
        }
        else
        {
            PlayerPrefs.SetString(PLAYER_NAME_KEY, playerName);
        }
    }

    public string GetPlayerName()
    {
        return playerName;
    }

    public bool AttemptSetPlayerName(string input)
    {
        bool results = true;
        if (input.Length >= 3 && input.Length <= 15)
        { 
            foreach(char e in input.ToCharArray())
            {
                if(!char.IsLetterOrDigit(e) && e != '_')
                {
                    results = false;
                    break;
                }
            }
        }
        else
        {
            results = false;
        }

        if(results)
        {
            playerName = input;
            PlayerPrefs.SetString(PLAYER_NAME_KEY, input);
        }

        return results;
    }

    #region lobby

    public async Task HostLobby()
    {
        string lobbyName = playerName+"'s Lobby";
        int maxPlayers = 10;
        CreateLobbyOptions options = new CreateLobbyOptions();
        options.IsPrivate = true;

        hostedLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);

        heartbeatCoroutine = StartCoroutine(HeartbeatLobbyCoroutine(hostedLobby.Id, 15));
    }

    IEnumerator HeartbeatLobbyCoroutine(string lobbyId, float waitTimeSeconds)
    {
        var delay = new WaitForSecondsRealtime(waitTimeSeconds);

        while (true)
        {
            Debug.Log("heartbeat");
            LobbyService.Instance.SendHeartbeatPingAsync(lobbyId);
            yield return delay;
        }
    }

        public string GetLobbyCode()
    {
        return hostedLobby.LobbyCode;
    }

    public string GetLobbySize()
    {
        return (10 - hostedLobby.AvailableSlots)+"/10";
    }

    public async Task<bool> AttemptJoinWithCode(string code)
    {
        bool success = true;

        try
        {
            await LobbyService.Instance.JoinLobbyByCodeAsync(code);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
            success = false;
        }

        return success;
    }

    #endregion
}
