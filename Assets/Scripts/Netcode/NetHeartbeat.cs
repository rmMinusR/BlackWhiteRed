using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class NetHeartbeat : NetworkBehaviour
{
    public float SmoothedRTT => _smoothedRtt.Value;
    [SerializeField] //TODO make inspector read-only
    protected NetworkVariable<float> _smoothedRtt = new NetworkVariable<float>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Owner);

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (IsOwner) heartbeatPingWorker = StartCoroutine(HeartbeatPingWorker());
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (heartbeatPingWorker != null)
        {
            StopCoroutine(heartbeatPingWorker);
            heartbeatPingWorker = null;
        }
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

    [SerializeField] [Range(2, 20)]
    public float heartbeatsPerSecond = 10;

    [SerializeField] [Min(1)] [Tooltip("How many pings should we use to find the average? Set to 1 to always use the most recent ping (not recommended.")]
    public int rttAverageCount = 30;

    [SerializeField] [Min(0.2f)] [Tooltip("How long until a ping should be considered failed? Measured in seconds.")]
    public double pingTimeout = 10;

    [SerializeField] protected List<CompletePing> pastPings = new List<CompletePing>(); // TODO Queue would be more efficient, but List shows in Inspector
    [SerializeField] protected List<OutgoingPing> travelingPings = new List<OutgoingPing>();

    [SerializeField] private Coroutine heartbeatPingWorker;
    private IEnumerator HeartbeatPingWorker()
    {
        // FIXME heartbeat might not keep connection alive in dedicated server mode
        while (true)
        {
            SendHeartbeatPing();

            //Remove timed-out pings
            travelingPings.RemoveAll(p => p.sendTime + pingTimeout < Time.realtimeSinceStartupAsDouble);

            yield return new WaitForSecondsRealtime(1/heartbeatsPerSecond);
        }
    }

    protected readonly static System.Random pingRNG = new System.Random();
    protected virtual void SendHeartbeatPing()
    {
        OutgoingPing ping = new OutgoingPing { id = pingRNG.Next(), sendTime = Time.realtimeSinceStartupAsDouble };
        travelingPings.Add(ping);

        ServerRpcParams p = new ServerRpcParams();
        p.Receive.SenderClientId = NetworkManager.Singleton.LocalClientId;

        Ping_ServerRpc(ping.id, p);
    }

    [ServerRpc(Delivery = RpcDelivery.Unreliable, RequireOwnership = false)]
    protected virtual void Ping_ServerRpc(int id, ServerRpcParams src)
    {
        ClientRpcParams dst = new ClientRpcParams();
        dst.Send.TargetClientIds = ClientIDCache.Narrowcast(src.Receive.SenderClientId); // Alloc-free version of new ulong[] { src.Receive.SenderClientId };

        Pong_ClientRpc(id, dst);
    }

    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    protected virtual void Pong_ClientRpc(int id, ClientRpcParams src)
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
            complete.rtt = complete.recieveTime-complete.sendTime;

            pastPings.Add(complete);
            while (pastPings.Count > rttAverageCount) pastPings.RemoveAt(0);

            RecalcAvgRTT();
        }
        else Debug.LogWarning("PING packet recieved twice: " + id);
    }

    protected void RecalcAvgRTT()
    {
        _smoothedRtt.Value = pastPings.Average(c => (float)c.rtt);
    }
}
