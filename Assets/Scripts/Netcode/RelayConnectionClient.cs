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

    private JoinAllocation allocationConnection = null;
    private string joinCode = null;
    private RelayServerEndpoint endpoint = null;

    protected internal override async Task ConnectToAllocation()
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

    protected internal override async Task ConnectTransport()
    {
        if (allocationConnection != null && endpoint != null)
        {
            NetworkManager.Singleton.ConnectionApprovalCallback -= OnConnectionApproved;
            NetworkManager.Singleton.ConnectionApprovalCallback += OnConnectionApproved;

            ((UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport).SetClientRelayData(endpoint.Host, (ushort)endpoint.Port, allocationConnection.AllocationIdBytes, allocationConnection.Key, allocationConnection.ConnectionData, allocationConnection.HostConnectionData);
            NetworkManager.Singleton.StartClient();

            while (!NetworkManager.Singleton.IsConnectedClient) await Task.Delay(100);
        }
        else throw new InvalidOperationException("Cannot connect transport before resolving allocation/endpoint!");
    }

    private void OnConnectionApproved(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        if (response.Approved)
        {
            _status = Status.NGOGood;
            //respond(true, null, true, Vector3.zero, null);
        }
        else
        {
            _status = Status.Rejected;
        } 
    }

    protected internal override async Task LoadScenes()
    {
        SceneGroupLoader.LoadOp op = GameManager.Instance.LoadMatchScenes(true, false);
        while (!op.isDone) await Task.Delay(100);
    }

    protected internal override void Close()
    {
        if (NetworkManager.Singleton)
        {
            _status = Status.NotConnected;
            NetworkManager.Singleton.ConnectionApprovalCallback -= OnConnectionApproved;

            NetworkManager.Singleton.Shutdown();

            //Discard state to prevent covariants
            allocationConnection = null;
            joinCode = null;
            endpoint = null;
        }
    }
}
