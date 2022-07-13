using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    Transform[] spawnPoints;

    [Space]
    [InspectorReadOnly]
    public PlayerController localPlayerController;

    public delegate void TriggerEvent();
    public static event TriggerEvent onMatchStart;

    public delegate void TeamEvent(Team team);
    public static event TeamEvent onTeamScore;
    public static event TeamEvent onTeamWin;

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
        spawnPoints = new Transform[2];
    }

    [ServerRpc(Delivery = RpcDelivery.Reliable, RequireOwnership = false)]
    public void LogAsReadyServerRpc(ulong clientId)
    {
        readyClientIds.Add(clientId);
        if (readyClientIds.Count == LobbyManager.Instance.GetNumberPlayers())
        {
            Debug.Log("ALL PLAYERS READY");
            StartMatch();
        }
    }

    void StartMatch()
    {
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
            NetworkManager.Singleton.ConnectedClients[readyClientIds[i]].PlayerObject.GetComponent<PlayerController>().AssignTeamClientRpc(teams[i], spawnPoints[(int)teams[i]].position, spawnPoints[(int)teams[i]].forward);
            NetworkManager.Singleton.ConnectedClients[readyClientIds[i]].PlayerObject.GetComponent<PlayerController>().ResetToSpawnPoint();
        }

        //Set Scores
        teamScores = new int[] { 0, 0 };

        //Attempting to make sure the server has these new transforms, but they're still getting reset.
        ForcePlayerControllers();

        //Send players to starting locations
        OnMatchStartClientRpc();
    }

    public void SetSpawnPoint(Team team, Transform point)
    {
        if (team == Team.INVALID)
        {
            return;
        }

        spawnPoints[(int)team] = point;
    }

    public void HandlePortalScore(PlayerController pc)
    {
        //Increase Score
        teamScores[pc.TeamValue]++;

        //Check For Win
        //TODO: Invoke events that things like the sound and UI will be listening for
        if (teamScores[pc.TeamValue] >= scoreToWin)
        {
            OnTeamWinClientRpc(pc.Team);
            Debug.Log(pc.Team + " WON");
        }
        else
        {
            OnTeamScoreClientRpc(pc.Team);
            Debug.Log(pc.Team + " SCORED");
        }


        //Attempting to make sure the server has these new transforms, but they're still getting reset.
        ForcePlayerControllers();
    }

    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    private void OnMatchStartClientRpc()
    {
        NetworkManager.Singleton.LocalClient.PlayerObject.TryGetComponent<PlayerController>(out localPlayerController);
        onMatchStart?.Invoke();
    }

    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    private void OnTeamScoreClientRpc(Team team)
    {
        onTeamScore?.Invoke(team);
    }

    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    private void OnTeamWinClientRpc(Team team)
    {
        onTeamWin?.Invoke(team);
    }

    private void ForcePlayerControllers()
    {
        for (int i = 0; i < readyClientIds.Count; i++)
        {
            NetworkManager.Singleton.ConnectedClients[readyClientIds[i]].PlayerObject.GetComponent<PlayerController>().ResetToSpawnPoint();
        }
    }
}
