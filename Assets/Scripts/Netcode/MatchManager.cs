using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public enum Team
{
    BLACK = 0,
    WHITE = 1,
    INVALID = 2
}

public class MatchManager : NetworkBehaviour
{
    [SerializeField]
    [Min(1)]
    public int scoreToWin;
    [SerializeField]
    [InspectorReadOnly]
    public int[] teamScores;
    [Space]
    [SerializeField]
    List<ulong> readyClientIds;
    [Space]
    [SerializeField]
    SpawnPointMarker[] spawnPoints;

    [Space]
    [SerializeField]
    public PlayerController localPlayerController;

    public delegate void TriggerEvent();
    public static event TriggerEvent onMatchStart;

    public delegate void TeamEvent(Team team);
    public static event TeamEvent onTeamScore;
    public static event TeamEvent onTeamWin;
    public static event Action<PlayerController> serverside_onScore;
    public static event Action<Team>             serverside_onTeamWin;

    public static MatchManager Instance;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Init();
            return;
        }

        Debug.LogError("Match Manager Instance already exists, deleting " + this.name);
        Destroy(this);
    }

    private void Init()
    {
        spawnPoints = new SpawnPointMarker[2];
    }

    [ServerRpc(Delivery = RpcDelivery.Reliable, RequireOwnership = false)]
    public void LogAsReadyServerRpc(ulong clientId)
    {
        Debug.Log($"Client {clientId} ready");
        readyClientIds.Add(clientId);
        if (readyClientIds.Count == LobbyManager.Instance.GetNumberPlayers())
        {
            Debug.Log("ALL PLAYERS READY");
            StartMatch();
        }
    }

    void StartMatch()
    {
        Debug.Log("Starting match");

        //Randomize Teams
        List<Team> teams = new List<Team>();
        int playerCount = readyClientIds.Count;
        for (int i = 0; i < playerCount - (playerCount % 2); i++)
        {
            teams.Add((Team)(i % 2));
        }
        for (int i = teams.Count - 1; i >= 1; i--)
        {
            int j = Random.Range(0, i + 1);
            Team temp = teams[i];
            teams[i] = teams[j];
            teams[j] = temp;
        }
        if (playerCount % 2 == 1)
        {
            teams.Add((Team)Random.Range(0, 2));
        }

        //Assign Player Objects to Teams
        for (int i = 0; i < playerCount; i++)
        {
            NetworkManager.Singleton.ConnectedClients[readyClientIds[i]].PlayerObject.GetComponent<PlayerController>().AssignTeamClientRpc(teams[i], spawnPoints[(int)teams[i]].transform.position, spawnPoints[(int)teams[i]].look);
            NetworkManager.Singleton.ConnectedClients[readyClientIds[i]].PlayerObject.GetComponent<PlayerController>().ResetToSpawnPoint();
        }

        //Set Scores
        teamScores = new int[] { 0, 0 };

        OnMatchStartClientRpc();
        StartRound();
        //FIXME: This is technically a race condition!
    }

    public void SetSpawnPoint(Team team, SpawnPointMarker point)
    {
        if (team == Team.INVALID)
        {
            return;
        }

        spawnPoints[(int)team] = point;
    }

    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    private void OnMatchStartClientRpc()
    {
        localPlayerController = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerController>();
        onMatchStart?.Invoke();
    }

    public void HandlePortalScore(PlayerController pc)
    {
        //Increase Score
        teamScores[pc.TeamValue]++;

        //Send message to all (including self!)
        MsgAll_TeamScored(pc.CurrentTeam, teamScores[pc.TeamValue], pc);
    }

    private void MsgAll_TeamScored(Team team, int newScore, PlayerController whoScored)
    {
        __HandleMsg_TeamScored(team, newScore, whoScored);
        __MsgClients_OnTeamScoreClientRpc(team, newScore, whoScored);
    }

    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    private void __MsgClients_OnTeamScoreClientRpc(Team team, int newScore, PlayerController whoScored)
    {
        if (!IsHost) __HandleMsg_TeamScored(team, newScore, whoScored);
    }

    private (PlayerController player, Team team) mostRecentlyScored; //Stored for the Animation
    private void __HandleMsg_TeamScored(Team team, int newScore, PlayerController whoScored)
    {
        mostRecentlyScored = (whoScored, team);

        //Run callbacks
        if (newScore < scoreToWin) onTeamScore?.Invoke(team);
        else                       onTeamWin  ?.Invoke(team);

        //Start round-end animation
        EndRound();
    }

    #region Round management

    //Animations trigger the actual 'work' functions on a timer
    [Header("Animations")]
    [SerializeField] private Animator roundAnimationHandler;
    [SerializeField] private string roundStartTriggerBinding;
    [SerializeField] private string roundEndTriggerBinding;

    #region Round start and end
    public void StartRound()
    {
        if (!IsServer) throw new AccessViolationException();

        Debug.Log("[Server] Sending start-round signal");

        //Message all (including self) exactly once
        __HandleMsg_StartRound();
        __MsgClients_StartRoundClientRpc();
    }

    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    private void __MsgClients_StartRoundClientRpc()
    {
        if (!IsHost) __HandleMsg_StartRound();
    }

    private void __HandleMsg_StartRound()
    {
        Debug.Log("Playing start-round animation");

        roundAnimationHandler.SetTrigger(roundStartTriggerBinding);
    }

    //No need for message-all idiom since it's called from HandleMsg_TeamScored instead, and would be a race condition if we did
    public void EndRound() => roundAnimationHandler.SetTrigger(roundEndTriggerBinding);
    #endregion

    #region 'Work' functions called from AnimationClips

    [Header("Announcements")]
    [SerializeField] private AnnouncementBannerDriver announcementBanner;
    [SerializeField] private string announcementTeamScored = "{0} team scored!";
    [SerializeField] private string announcementTeamWon = "{0} team won!";
    [SerializeField] [Min(0)] private float announcementCountdownShowTime = 1f;

    /// <summary>
    /// State change requires server authority - does nothing if not server
    /// </summary>
    public void ANIM_MovePlayersToSpawn()
    {
        if (!IsServer) return;

        foreach (NetworkClient c in NetworkManager.Singleton.ConnectedClientsList) c.PlayerObject.GetComponent<PlayerController>().ResetToSpawnPoint();
    }

    /// <summary>
    /// UI - fires for all clients
    /// </summary>
    public void ANIM_ShowTeamScoredBanner(float time)
    {
        if (!IsClient) return;

        bool isWin = teamScores[(int)mostRecentlyScored.team] >= scoreToWin;
        string toShow = string.Format(isWin ? announcementTeamWon : announcementTeamScored, mostRecentlyScored.team.ToString());
        announcementBanner.Show(toShow, time);
    }

    /// <summary>
    /// UI - fires for all clients
    /// </summary>
    public void ANIM_ShowCountdownBanner(string text)
    {
        if (!IsClient) return;

        announcementBanner.Close();
        announcementBanner.Show(text, announcementCountdownShowTime);
    }

    /// <summary>
    /// State change requires server authority - does nothing if not server
    /// </summary>
    public void ANIM_MoveToNextRoundOrLobby()
    {
        if (!IsServer) return;

        bool keepPlaying = teamScores[(int)mostRecentlyScored.team] < scoreToWin;

        if (keepPlaying)
        {
            if (IsServer) StartRound();
        }
        else __ReturnToLobby();
    }

    /// <summary>
    /// State change, but no server authority required. FIXME?
    /// </summary>
    public void ANIM_SetTimescale(float scale)
    {
        Time.timeScale = scale; //Keep UI time
        Time.fixedDeltaTime = 1/50f * scale; //Adjust physics time
    }

    private void __ReturnToLobby() => throw new NotImplementedException(); //TODO

    #endregion

    #endregion
}