using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Services.Authentication;
using UnityEngine;

public class PlayerAuthenticationManager
{
    //Player Customization
    public const string PLAYER_NAME_KEY = "PLAYERNAME";
    private FixedString128Bytes playerName = "Shade";

    //Authentication
    private string playerId = "Not signed in";
    private string accessToken = "No access token";

    private static PlayerAuthenticationManager instance;
    public static PlayerAuthenticationManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new PlayerAuthenticationManager();
            }
            return instance;
        }
    }

    public async Task AttemptSignIn()
    {
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        playerId = AuthenticationService.Instance.PlayerId;
        accessToken = AuthenticationService.Instance.AccessToken;
    }

    public string GetPlayerID()
    {
        return playerId;
    }

    #region player_customization
    public void CheckForPrefs()
    {
        if (PlayerPrefs.HasKey(PLAYER_NAME_KEY))
        {
            playerName = PlayerPrefs.GetString(PLAYER_NAME_KEY);
        }
        else
        {
            PlayerPrefs.SetString(PLAYER_NAME_KEY, playerName.ConvertToString());
        }
    }

    public FixedString128Bytes GetPlayerName()
    {
        return playerName;
    }

    public bool AttemptSetPlayerName(string input)
    {
        bool results = true;
        if (input.Length >= 3 && input.Length <= 15)
        {
            foreach (char e in input.ToCharArray())
            {
                if (!char.IsLetterOrDigit(e) && e != '_')
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

        if (results)
        {
            playerName = input;
            PlayerPrefs.SetString(PLAYER_NAME_KEY, input);
        }

        return results;
    }
    #endregion

}
