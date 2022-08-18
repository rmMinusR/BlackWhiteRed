using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public class MatchBeginHelper : NetworkBehaviour
{
    private async void Start()
    {
        while (!IsSpawned)
        {
            await Task.Delay(30);
            if (NetworkManager.Singleton.IsServer)
            {
                foreach (NetworkObject o in FindObjectsOfType<NetworkObject>()) if (!o.IsSpawned) o.Spawn(destroyWithScene: true);
            }
        }
    }

    [SerializeField] private List<ulong> loadedClientIds = new List<ulong>();
    public IReadOnlyList<ulong> LoadedClientIds => loadedClientIds;
    public bool AllClientsLoaded => NetworkManager.Singleton?.ConnectedClientsIds.All(id => loadedClientIds.Contains(id)) ?? false;

    public void ReportLoadComplete()
    {
        StartCoroutine(__ReportLoadCompleteWorker());
    }

    private IEnumerator __ReportLoadCompleteWorker()
    {
        while(true)
        {
            Debug.Log("Sending ready signal...");
            ReportLoadCompleteServerRpc();
            yield return new WaitForSecondsRealtime(0.2f);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ReportLoadCompleteServerRpc(ServerRpcParams p = default)
    {
        Debug.Log($"Client {p.Receive.SenderClientId} ready");
        loadedClientIds.Add(p.Receive.SenderClientId);

        ConfirmConnectionClientRpc(p.ReturnToSender());
    }

    [ClientRpc]
    private void ConfirmConnectionClientRpc(ClientRpcParams p)
    {
        Debug.Log("Confirmed connection established");
        StopAllCoroutines();
        //FIXME Destroy
    }
}
