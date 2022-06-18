using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

[RequireComponent(typeof(RelayManager))]
public sealed class RelayConnectionClient : BaseRelayConnection
{
    public string tmpJoinCode;

    private async void Start()
    {
        await ConnectToAllocation(tmpJoinCode);
        ConnectTransport();
    }

    private void OnDestroy()
    {
        Close();
    }

    private void OnApplicationQuit()
    {
        Close();
    }

    private JoinAllocation allocationConnection = null;
    private string joinCode = null;
    private RelayServerEndpoint endpoint = null;

    private async Task ConnectToAllocation(string joinCode)
    {
        // TODO safety assert, validate that we should create a client connection (aren't already hosting)

        //TESTING ONLY
        if (UnityServices.State == ServicesInitializationState.Uninitialized) await UnityServices.InitializeAsync();

        //TESTING ONLY
        if (!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();

        if (allocationConnection == null)
        {
            allocationConnection = await RelayService.Instance.JoinAllocationAsync(joinCode);
            Debug.Assert(allocationConnection != null, "Failed to create Relay allocation");

            this.joinCode = joinCode;
        }

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
        if (allocationConnection != null && endpoint != null)
        {
            ((UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport).SetClientRelayData(endpoint.Host, (ushort)endpoint.Port, allocationConnection.AllocationIdBytes, allocationConnection.Key, allocationConnection.ConnectionData, allocationConnection.HostConnectionData);
            NetworkManager.Singleton.StartClient();
        }
        else throw new System.InvalidOperationException("Cannot connect transport before resolving allocation/endpoint!");
    }

    private void Close()
    {
        NetworkManager.Singleton.Shutdown();
    }
}
