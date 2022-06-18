using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

[RequireComponent(typeof(RelayManager))]
public sealed class RelayConnectionHost : RelayConnection
{
    private async void Start()
    {
        await ConnectToAllocation();
        Debug.Log(joinCode);
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

    private void OnGUI()
    {
        GUILayout.Label("Joincode: "+joinCode);
    }

    private Allocation allocation = null;
    private string joinCode = null;
    private RelayServerEndpoint endpoint = null;

    private async Task ConnectToAllocation()
    {
        // TODO safety assert, validate that we should be the host

        //TESTING ONLY
        if (UnityServices.State == ServicesInitializationState.Uninitialized) await UnityServices.InitializeAsync();

        //TESTING ONLY
        if (!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();
        
        if (allocation == null)
        {
            allocation = await RelayService.Instance.CreateAllocationAsync(RelayManager.Instance.maxConnections - 1); // Number of *peers*, does not include host - FIXME special case for server vs integrated host?
            Debug.Assert(allocation != null, "Failed to create Relay allocation");
        }

        if (joinCode == null)
        {
            if (allocation != null)
            {
                joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                Debug.Assert(joinCode != null, "Failed to create Relay joincode");
            }
            else Debug.LogWarning("Cannot create joincode without allocation");
        }

        if (endpoint == null)
        {
            if (allocation != null)
            {
                endpoint = RelayManager.SelectEndpoint(allocation.ServerEndpoints);
                Debug.Assert(endpoint != null, "Failed to select a Relay endpoint");
            }
            else Debug.LogWarning("Cannot locate endpoint without allocation");
        }
    }

    private void ConnectTransport()
    {
        if (allocation != null && endpoint != null)
        {
            ((UnityTransport) NetworkManager.Singleton.NetworkConfig.NetworkTransport).SetHostRelayData(endpoint.Host, (ushort)endpoint.Port, allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData);
            NetworkManager.Singleton.StartHost();
        }
        else throw new System.InvalidOperationException("Cannot connect transport before resolving allocation/endpoint!");
    }

    private void Close()
    {
        NetworkManager.Singleton.Shutdown();
    }
}
