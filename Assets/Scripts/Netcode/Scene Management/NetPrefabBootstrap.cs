using System.Collections;
using Unity.Netcode;
using UnityEngine;

public sealed class NetPrefabBootstrap : MonoBehaviour
{
    [SerializeField] private NetworkObject prefab;

    private void Update()
    {
        if (NetworkManager.Singleton.IsServer) Instantiate(prefab).Spawn();

        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer) Destroy(gameObject);
    }
}
