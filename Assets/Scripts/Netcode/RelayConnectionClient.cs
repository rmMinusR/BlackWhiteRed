using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

[RequireComponent(typeof(RelayManager))]
public sealed class RelayConnectionClient : RelayConnection
{
    private JoinAllocation allocationConnection = null;
    private string joinCode = null;
    private RelayServerEndpoint endpoint = null;

    private async void ConnectToAllocation(string joinCode)
    {
        // TODO safety assert, validate that we should create a client connection (aren't already hosting)

        if (allocationConnection == null)
        {
            allocationConnection = await RelayService.Instance.JoinAllocationAsync(joinCode);
            Debug.Assert(allocationConnection != null, "Failed to create Relay allocation");
        }

        this.joinCode = joinCode;

        if (endpoint == null)
        {
            if (allocationConnection != null)
            {
                endpoint = RelayManager.SelectEndpoint(allocationConnection.ServerEndpoints);
                Debug.Assert(endpoint != null, "Failed to select a Relay endpoint");
            }
            else Debug.LogWarning("Cannot locate endpoint without allocation");
        }
    }

    private void ConnectTransport()
    {
        if (allocation != null && endpoint != null)
        {
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(endpoint.Host, (ushort)endpoint.Port, allocationConnection.AllocationIdBytes, allocationConnection.Key, allocationConnection.ConnectionData, allocationConnection.HostConnectionData);
            NetworkManager.Singleton.StartClient();
        }
        else throw new System.InvalidOperationException("Cannot connect transport before resolving allocation/endpoint!");
    }
}
