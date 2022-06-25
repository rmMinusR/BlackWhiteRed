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
    List<ulong> readyClientIds;

    public static MatchManager Instance;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            return;
        }

        Debug.LogError("Match Manager Instance already exists, deleting "+ this.name);
        Destroy(this);
    }

    [ServerRpc(Delivery = RpcDelivery.Reliable,RequireOwnership = false)]
    public void LogAsReadyServerRpc(ulong clientId) 
    {
        readyClientIds.Add(clientId);
        if(readyClientIds.Count == LobbyManager.Instance.GetNumberPlayers())
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
        for(int i = 0; i < playerCount - (playerCount % 2); i++)
        {
            teams.Add((Team)(i % 2));
        }
        for(int i = teams.Count - 1; i >= 1; i--)
        {
            int j = Random.Range(0, i + 1);
            Team temp = teams[i];
            teams[i] = teams[j];
            teams[j] = temp;
        }
        if(playerCount % 2 == 1)
        {
            teams.Add((Team)Random.Range(0, 2));
        }

        //Assign Player Objects to Teams
        for(int i = 0; i < playerCount; i++)
        {
            NetworkManager.Singleton.ConnectedClients[readyClientIds[i]].PlayerObject.GetComponent<PlayerController>().AssignTeamClientRpc(teams[i]);
        }
    }
}
