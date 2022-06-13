using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Services.Core;
using Unity.Services.Authentication;
using System.Threading.Tasks;

public class GameManager : MonoBehaviour
{
    //Player Customization
    private const string PLAYER_NAME_KEY = "PLAYERNAME";
    private string playerName = "[Shade]";

    //Authentication
    private string playerId = "Not signed in";
    private string accessToken = "No access token";

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
}
