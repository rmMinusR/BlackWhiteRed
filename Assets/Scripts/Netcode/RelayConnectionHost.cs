using System;
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
public sealed class RelayConnectionHost : BaseRelayConnection
{
    public static RelayConnectionHost New(GameObject gameObject)
    {
        if (gameObject.TryGetComponent<BaseRelayConnection>(out _)) throw new InvalidOperationException("Cannot create a connection, since one already exists!");

        return gameObject.AddComponent<RelayConnectionHost>();
    }

    private async void Start()
    {
        await ConnectToAllocation();
        Debug.Log(joinCode);
        ConnectTransport();
    }

    private void _OnClientConnected(ulong id)
    {
        Debug.Log("Player "+id+" connected");
    }

    private void _OnClientDisconnected(ulong id)
    {
        Debug.Log("Player " + id + " disconnected");
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
        if (GUILayout.Button("Copy"))
        {
            GUIUtility.systemCopyBuffer = joinCode;
        }
    }

    private Allocation allocation = null;
    private string joinCode = null;
    public string JoinCode => joinCode;
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
                endpoint = SelectEndpoint(allocation.ServerEndpoints);
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

            NetworkManager.Singleton.OnClientConnectedCallback -= _OnClientConnected;
            NetworkManager.Singleton.OnClientConnectedCallback += _OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= _OnClientDisconnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += _OnClientDisconnected;
        }
        else throw new System.InvalidOperationException("Cannot connect transport before resolving allocation/endpoint!");
    }

    private void Close()
    {
        if (NetworkManager.Singleton)
        {
            //Disconnect clients from NGO
            List<ulong> clients = new List<ulong>(NetworkManager.Singleton.ConnectedClientsIds);
            foreach (ulong client in clients) NetworkManager.Singleton.DisconnectClient(client);

            //Disconnect clients from Relay
            //TODO

            System.Threading.Thread.Sleep(50); //Give time for disconnect signal to go out

            NetworkManager.Singleton.OnClientConnectedCallback -= _OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= _OnClientDisconnected;

            NetworkManager.Singleton.Shutdown();
        }
    }
}
