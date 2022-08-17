using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public class MatchBeginHelper : NetworkBehaviour
{
    public SceneLoadMonitor loadOverlay;

    //Single-call RPC idiom
    public void BeginMatch()
    {
        if (!IsServer) throw new AccessViolationException();

        //Message all (including self) exactly once
        __HandleMsg_BeginMatch();
        __MsgClients_BeginMatchClientRpc();
    }

    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    private void __MsgClients_BeginMatchClientRpc()
    {
        if (!IsHost) __HandleMsg_BeginMatch();
    }

    const string SceneNamePlayers = "Level3-Area0-Players";
    const string SceneNameLevelDesign = "Level3-Area0-LevelDesign";
    const string SceneNameEnvironmentArt = "Level3-Area0-EnvironmentArt";

    private void __HandleMsg_BeginMatch()
    {
        SceneGroupLoader.LoadOp progress = SceneGroupLoader.Instance.LoadSceneGroupAsync(SceneNameLevelDesign, SceneNameEnvironmentArt, SceneNamePlayers).progress;

        if (IsClient) progress.onComplete += () => __ReportLoadCompleteServerRpc();
        if (IsServer) progress.onComplete += () => MatchManager.Instance.StartWhenPlayersLoaded();

        //Send progress monitor to UI
        loadOverlay.Monitor(progress);
    }

    [SerializeField] private List<ulong> loadedClientIds = new List<ulong>();
    public IReadOnlyList<ulong> LoadedClientIds => loadedClientIds;
    public bool AllClientsLoaded => NetworkManager.ConnectedClientsIds.All(id => loadedClientIds.Contains(id));

    [ServerRpc]
    private void __ReportLoadCompleteServerRpc(ServerRpcParams p = default)
    {
        Debug.Log($"Client {p.Receive.SenderClientId} ready");
        loadedClientIds.Add(p.Receive.SenderClientId);
    }
}
