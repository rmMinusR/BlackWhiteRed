using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class NetHeartbeat : NetworkBehaviour
{
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

    [SerializeField] [Range(2, 20)] public float heartbeatsPerSecond = 10;
    [SerializeField] [Min(1)] public int rttHistory = 30;

    [SerializeField] protected List<CompletePing> pastPings = new List<CompletePing>(); // TODO Queue would be more efficient, but List shows in Inspector
    [SerializeField] protected List<OutgoingPing> travelingPings = new List<OutgoingPing>();

    [SerializeField] private Coroutine heartbeatPingWorker;
    private IEnumerator HeartbeatPingWorker()
    {
        // FIXME heartbeat might not keep connection alive in dedicated server mode
        while (true)
        {
            SendHeartbeatPing();

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
}
