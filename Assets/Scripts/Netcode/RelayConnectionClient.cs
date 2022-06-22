using System;
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
    public static RelayConnectionClient New(GameObject gameObject, string joinCode)
    {
        if (gameObject.TryGetComponent<BaseRelayConnection>(out _)) throw new InvalidOperationException("Cannot create a connection, since one already exists!");

        RelayConnectionClient c = gameObject.AddComponent<RelayConnectionClient>();
        c.joinCode = joinCode;
        return c;
    }

    [SerializeField] private Status _status = Status.NotConnected; //TODO make inspector readonly
    public override Status GetStatus() => _status;

    private async void Start()
    {
        await ConnectToAllocation();
        ConnectTransport();
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        Close();
    }

    private void OnApplicationQuit()
    {
        Close();
    }

    private JoinAllocation allocationConnection = null;
    private string joinCode = null;
    private RelayServerEndpoint endpoint = null;

    private async Task ConnectToAllocation()
    {
        // TODO safety assert, validate that we should create a client connection (aren't already hosting)

        //TESTING ONLY
        if (UnityServices.State == ServicesInitializationState.Uninitialized) await UnityServices.InitializeAsync();

        //TESTING ONLY
        if (!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();

        _status = Status.AuthGood;

        if (allocationConnection == null)
        {
            allocationConnection = await RelayService.Instance.JoinAllocationAsync(joinCode);
            Debug.Assert(allocationConnection != null, "Failed to create Relay allocation");
        }

        if (endpoint == null)
        {
            if (allocationConnection != null)
            {
                endpoint = SelectEndpoint(allocationConnection.ServerEndpoints);
                Debug.Assert(endpoint != null, "Failed to select a Relay endpoint");
            }
            else Debug.LogWarning("Cannot locate endpoint without allocation");
        }

        _status = Status.RelayGood;
    }

    private void ConnectTransport()
    {
        if (allocationConnection != null && endpoint != null)
        {
            NetworkManager.Singleton.ConnectionApprovalCallback -= Callback_ConfirmConnection;
            NetworkManager.Singleton.ConnectionApprovalCallback += Callback_ConfirmConnection;

            ((UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport).SetClientRelayData(endpoint.Host, (ushort)endpoint.Port, allocationConnection.AllocationIdBytes, allocationConnection.Key, allocationConnection.ConnectionData, allocationConnection.HostConnectionData);
            NetworkManager.Singleton.StartClient();

        }
        else throw new InvalidOperationException("Cannot connect transport before resolving allocation/endpoint!");
    }

    private void Callback_ConfirmConnection(byte[] arg1, ulong arg2, NetworkManager.ConnectionApprovedDelegate arg3)
    {
        _status = Status.NGOGood; //TODO check if rejected by server
    }
    
    private void Close()
    {
        if (NetworkManager.Singleton)
        {
            _status = Status.NotConnected;
            NetworkManager.Singleton.ConnectionApprovalCallback -= Callback_ConfirmConnection;

            NetworkManager.Singleton.Shutdown();

            //Discard state to prevent covariants
            allocationConnection = null;
            joinCode = null;
            endpoint = null;
        }
    }
}
