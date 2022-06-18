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

    [SerializeField] private BaseRelayConnection _connection; //TODO make inspector read-only
    public BaseRelayConnection Connection => _connection;

    [SerializeField] [Range(5, 20)] public float heartbeatsPerSecond = 10;
    [SerializeField] [Min(1)] public int rttHistory = 30;

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

    public void StartAsHost()
    {
        if (_connection) throw new InvalidOperationException("Already running!");

        _connection = gameObject.AddComponent<RelayConnectionHost>();
    }

    public void StartAsClient(string relayJoinCode)
    {
        if (_connection) throw new InvalidOperationException("Already running!");

        RelayConnectionClient c = gameObject.AddComponent<RelayConnectionClient>();
        _connection = c;
        c.tmpJoinCode = relayJoinCode;
    }

    #region Testing-only code

    private string relayManualJoinCode;
    private void OnGUI()
    {
        if (Connection == null)
        {
            if(GUILayout.Button("               Host               ")) StartAsHost();

            GUILayout.BeginHorizontal();
            relayManualJoinCode = GUILayout.TextField(relayManualJoinCode);
            bool launchClient = GUILayout.Button("Client");
            GUILayout.EndHorizontal();

            if(launchClient) StartAsClient(relayManualJoinCode);
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

    #endregion
}

[RequireComponent(typeof(RelayManager))]
public abstract class BaseRelayConnection : MonoBehaviour
{
    protected RelayManager manager;

    #region Heartbeat / RTT

    protected virtual void OnEnable()
    {
        manager = GetComponent<RelayManager>();
        heartbeatPingWorker = StartCoroutine(HeartbeatPingWorker());
    }

    protected virtual void OnDisable()
    {
        StopCoroutine(heartbeatPingWorker);
        heartbeatPingWorker = null;
    }

    [Serializable]
    protected struct CompletePing
    {
        public int id;
        public double sendTime;
        public double recieveTime;
        public double rtt;
    }

    [Serializable]
    protected struct OutgoingPing
    {
        public int id;
        public double sendTime;
    }

    [SerializeField] protected List<CompletePing> pastPings = new List<CompletePing>(); // TODO Queue would be more efficient, but List shows in Inspector
    [SerializeField] protected List<OutgoingPing> travelingPings = new List<OutgoingPing>();

    [SerializeField] private Coroutine heartbeatPingWorker;
    protected virtual IEnumerator HeartbeatPingWorker()
    {
        // FIXME heartbeat might not work in dedicated server mode
        while (true)
        {
            SendHeartbeatPing();

            yield return new WaitForSecondsRealtime(1/manager.heartbeatsPerSecond);
        }
    }

    protected readonly static System.Random pingRNG = new System.Random();
    private void SendHeartbeatPing()
    {
        OutgoingPing ping = new OutgoingPing { id = pingRNG.Next(), sendTime = Time.realtimeSinceStartupAsDouble };
        travelingPings.Add(ping);
        Ping(ping.id);
    }

    [ServerRpc]
    protected virtual void Ping(int id) => Pong(id);

    [ClientRpc]
    protected virtual void Pong(int id)
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
            while (pastPings.Count > manager.rttHistory) pastPings.RemoveAt(0);
        }
        else Debug.LogWarning("PING packet recieved twice: " + id);
    }

    #endregion
}
