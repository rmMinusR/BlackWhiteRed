using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Relay.Models;
using UnityEngine;

[RequireComponent(typeof(RelayManager))]
public abstract class BaseRelayConnection : MonoBehaviour
{
    //protected static RelayServerEndpoint SelectEndpoint(List<RelayServerEndpoint> endpoints) => endpoints.Find(e => e.ConnectionType == "dtls"); // RSC 6/21/22: DTLS encryption not working
    protected static RelayServerEndpoint SelectEndpoint(List<RelayServerEndpoint> endpoints) => endpoints.Find(e => e.ConnectionType == "udp"); // Use UDP unencrypted

    public abstract Status GetStatus();

    public enum Status
    {
        Rejected = -2,
        Failed = -1,
        NotConnected = 0,

        //Intermediate connection steps
        AuthGood,
        RelayGood,
        NGOGood,

        Connected = NGOGood //Tempfix - same state with different name in case we need to add more connection steps
    }
}