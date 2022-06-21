using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.Services.Relay.Models;
using UnityEngine;

[RequireComponent(typeof(RelayManager))]
public abstract class BaseRelayConnection : MonoBehaviour
{
    protected static RelayServerEndpoint SelectEndpoint(List<RelayServerEndpoint> endpoints) => endpoints.Find(e => e.ConnectionType == "dtls"); // Use DTLS encryption

    #region Heartbeat / RTT

    protected virtual void OnEnable()
    {
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
    private IEnumerator HeartbeatPingWorker()
    {
        // FIXME heartbeat might not work in dedicated server mode
        while (true)
        {
            SendHeartbeatPing();

            yield return new WaitForSecondsRealtime(1/RelayManager.Instance.heartbeatsPerSecond);
        }
    }

    protected readonly static System.Random pingRNG = new System.Random();
    protected virtual void SendHeartbeatPing()
    {
        OutgoingPing ping = new OutgoingPing { id = pingRNG.Next(), sendTime = Time.realtimeSinceStartupAsDouble };
        travelingPings.Add(ping);
        Ping(ping.id);
    }

    [ServerRpc(Delivery = RpcDelivery.Unreliable)]
    protected virtual void Ping(int id)
    {
        Pong(id);
        //Debug.Log("PING serverside #"+id);
    }

    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    protected virtual void Pong(int id)
    {
        //Debug.Log("PING clientside #" + id);

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
            while (pastPings.Count > RelayManager.Instance.rttHistory) pastPings.RemoveAt(0);
        }
        else Debug.LogWarning("PING packet recieved twice: " + id);
    }

    #endregion
}