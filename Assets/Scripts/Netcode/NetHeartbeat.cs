﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class NetHeartbeat : NetworkBehaviour
{
    #region Pseudo-singleton

    public static NetHeartbeat Self => NetHeartbeat.Of(NetworkManager.Singleton.LocalClientId);

    private static Dictionary<ulong, NetHeartbeat> __Instances = new Dictionary<ulong, NetHeartbeat>();
    public static NetHeartbeat Of(ulong playerID) => __Instances.TryGetValue(playerID, out NetHeartbeat i) ? i : throw new IndexOutOfRangeException();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!__Instances.TryAdd(OwnerClientId, this)) Debug.LogError("NetHeartbeat already registered for player "+OwnerClientId+"!");

        if (IsLocalPlayer)
        {
            heartbeatWorker = StartCoroutine(HeartbeatWorker());
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (!__Instances.Remove(OwnerClientId)) Debug.LogError("NetHeartbeat already unregistered for player "+OwnerClientId+"!");

        if (heartbeatWorker != null)
        {
            StopCoroutine(heartbeatWorker);
            heartbeatWorker = null;
        }
    }

    #endregion

    [SerializeField] [Range(2, 20)]
    private float heartbeatsPerSecond = 10;

    [SerializeField] [Min(1)] [Tooltip("How many pings should we use to find the average? Set to 1 to always use the most recent ping (not recommended.")]
    private int rttAverageCount = 30;

    [SerializeField] [Min(0.2f)] [Tooltip("How long until a ping should be considered failed? Measured in seconds.")]
    private double pingTimeout = 10;

    [Serializable]
    protected struct CompletePing
    {
        public int id;
        public double sendTime;
        public double recieveTime;
        public float rtt;
    }

    [Serializable]
    protected struct OutgoingPing
    {
        public int id;
        public double sendTime;
    }

    [SerializeField] protected List<CompletePing> pastPings = new List<CompletePing>(); // TODO Queue would be more efficient, but List shows in Inspector
    [SerializeField] protected List<OutgoingPing> travelingPings = new List<OutgoingPing>();

    public float SmoothedRTT => _smoothedRtt.Value;
    [SerializeField] //TODO make inspector read-only
    protected NetworkVariable<float> _smoothedRtt = new NetworkVariable<float>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Owner);

    private Coroutine heartbeatWorker;
    private IEnumerator HeartbeatWorker()
    {
        // FIXME heartbeat might not keep connection alive in dedicated server mode
        while (true)
        {
            SendHeartbeat();

            //Remove timed-out pings
            travelingPings.RemoveAll(p => p.sendTime + pingTimeout < Time.realtimeSinceStartupAsDouble);

            yield return new WaitForSecondsRealtime(1/heartbeatsPerSecond);
        }
    }

    protected readonly static System.Random pingRNG = new System.Random();
    protected virtual void SendHeartbeat()
    {
        OutgoingPing ping = new OutgoingPing { id = pingRNG.Next(), sendTime = Time.realtimeSinceStartupAsDouble };
        travelingPings.Add(ping);

        Heartbeat_ServerRpc(ping.id);
    }

    [ServerRpc(Delivery = RpcDelivery.Unreliable, RequireOwnership = false)]
    protected virtual void Heartbeat_ServerRpc(int pingID, ServerRpcParams src = default)
    {
        HeartbeatResponse_ClientRpc(pingID, Time.realtimeSinceStartup, src.ReturnToSender());  //TODO replace with match timer
    }

    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    protected virtual void HeartbeatResponse_ClientRpc(int id, float serverTime, ClientRpcParams src = default)
    {
        IEnumerable<OutgoingPing> query = travelingPings.Where(d => d.id == id);
        if(query.Any())
        {
            //Retrieve record and calculate RTT
            OutgoingPing received = query.First();
            travelingPings.Remove(received);

            CompletePing complete = new CompletePing()
            {
                id          = received.id,
                sendTime    = received.sendTime,
                recieveTime = Time.realtimeSinceStartupAsDouble,
            };
            complete.rtt = (float) (complete.recieveTime-complete.sendTime);

            pastPings.Add(complete);
            pastPings.RemoveRange(0, Mathf.Max(0, pastPings.Count-rttAverageCount));

            RecalcAvgRTT();

            //Recalculate serverside time
            //TODO should this be put in another RPC method?
            PushSyncTime(serverTime+complete.rtt/2);
        }
        else Debug.LogWarning("PING packet recieved twice: " + id);
    }

    protected void RecalcAvgRTT()
    {
        _smoothedRtt.Value = pastPings.Average(c => (float)c.rtt);
    }

    [SerializeField] [Min(1)] private int timeSyncAvgCount = 50;
    [SerializeField] private List<float> timeSyncDeltas = new List<float>();

    [SerializeField] //TODO make inspector read-only
    private float _smoothedTimeSyncDelta;

    protected void PushSyncTime(float timeOnServer)
    {
        //TODO should this be turned off or limited once some point of certainty is reached?
        //TODO standard deviation?

        timeSyncDeltas.Add(Time.realtimeSinceStartup-timeOnServer);
        timeSyncDeltas.RemoveRange(0, Mathf.Max(0, timeSyncDeltas.Count-timeSyncAvgCount));

        _smoothedTimeSyncDelta = timeSyncDeltas.Average();
    }

    public float ConvertTimeServerToLocal(float serverTime) => serverTime + _smoothedTimeSyncDelta;
    public float ConvertTimeLocalToServer(float localTime ) => localTime  - _smoothedTimeSyncDelta;
}
