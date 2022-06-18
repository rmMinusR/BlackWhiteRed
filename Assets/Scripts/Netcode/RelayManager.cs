using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class RelayManager : MonoBehaviour
{
    [SerializeField] [Min(0)] internal int maxConnections;

    public static RelayManager Instance { get; private set; }

    [SerializeField] private RelayConnection _connection; //TODO make inspector read-only
    public RelayConnection Connection => _connection;

    // Helper for RelayConnection
    internal static RelayServerEndpoint SelectEndpoint(List<RelayServerEndpoint> endpoints) => endpoints.Find(e => e.ConnectionType == "dtls"); // Use DTLS encryption

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Debug.LogError("Only one RelayManager may exist! Destroying "+this);
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        StartCoroutine(HeartbeatPingWorker());
    }

    #region Heartbeat / RTT

    [Serializable]
    private struct CompletePing
    {
        public int id;
        public double sendTime;
        public double recieveTime;
        public double rtt;
    }

    [Serializable]
    private struct OutgoingPing
    {
        public int id;
        public double sendTime;
    }

    [SerializeField] [Range(5, 20)] private float heartbeatsPerSecond = 1;
    [SerializeField] [Min(1)] private int rttHistory = 30;
    [SerializeField] private List<CompletePing> pastPings = new List<CompletePing>(); // TODO Queue would be more efficient, but List shows in Inspector
    [SerializeField] private List<OutgoingPing> travelingPings = new List<OutgoingPing>();

    private IEnumerator HeartbeatPingWorker()
    {
        // FIXME heartbeat might not work in dedicated server mode
        while (true)
        {
            if (Connection) SendHeartbeatPing();

            yield return new WaitForSecondsRealtime(1/heartbeatsPerSecond);
        }
    }

    private static System.Random pingRNG = new System.Random();
    private void SendHeartbeatPing()
    {
        OutgoingPing ping = new OutgoingPing { id = pingRNG.Next(), sendTime = Time.realtimeSinceStartupAsDouble };
        travelingPings.Add(ping);
        Ping(ping.id);
    }

    [ServerRpc]
    private void Ping(int id) => Pong(id);

    [ClientRpc]
    private void Pong(int id)
    {
        IEnumerable<OutgoingPing> query = travelingPings.Where(d => d.id == id);
        if(query.Any())
        {
            OutgoingPing received = query.First();
            travelingPings.Remove(received);

            CompletePing complete = new CompletePing()
            {
                id          = received.id,
                sendTime    = received.sendTime,
                recieveTime = Time.realtimeSinceStartupAsDouble,
            };
            complete.rtt = complete.recieveTime-complete.sendTime;

            pastPings.Add(complete);
            while (pastPings.Count > rttHistory) pastPings.RemoveAt(0);
        }
        else Debug.LogWarning("PING packet recieved twice: " + id);
    }

    #endregion

    private string relayManualJoinCode;
    private void OnGUI()
    {
        if (Connection == null)
        {
            if(GUILayout.Button("               Host               "))
            {
                _connection = gameObject.AddComponent<RelayConnectionHost>();
            }

            GUILayout.BeginHorizontal();
            relayManualJoinCode = GUILayout.TextField(relayManualJoinCode);
            bool launchClient = GUILayout.Button("Client");
            GUILayout.EndHorizontal();

            if(launchClient)
            {
                _connection = gameObject.AddComponent<RelayConnectionClient>();
                (_connection as RelayConnectionClient).tmpJoinCode = relayManualJoinCode;
            }
        }
        else
        {
            GUILayout.Space(50);
            if (GUILayout.Button("               Close               "))
            {
                Destroy(_connection);
                _connection = null;
            }
        }
    }
}

public abstract class RelayConnection : MonoBehaviour
{
}