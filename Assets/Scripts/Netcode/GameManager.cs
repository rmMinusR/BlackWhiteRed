using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Services.Core;
using Unity.Services.Authentication;
using System.Threading.Tasks;

public class GameManager : MonoBehaviour
{
    //private Guid hostAllocationId;
    //private Guid playerAllocationId;
    //private string allocationRegion = "";
    //private string joinCode = "n/a";
    //private string playerId = "Not signed in";
    //private string autoSelectRegionName = "auto-select (QoS)";
    //private int RegionAutoSelectIndex = 0;
    //private List<Region> regions = new List<Region>();
    //private List<string> regionOptions = new List<string>();

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
    }

    async Task AttemptSignIn()
    {
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        playerId = AuthenticationService.Instance.PlayerId;
        accessToken = AuthenticationService.Instance.AccessToken;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
