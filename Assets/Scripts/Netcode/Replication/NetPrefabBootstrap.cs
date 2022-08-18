using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Awful fix for NetworkObjects in scene having different ID hashes
/// </summary>
public class NetPrefabBootstrap : MonoBehaviour
{
    [SerializeField] private NetworkObject prefab;

    private void Awake()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            NetworkObject o = Instantiate(prefab, transform.position, transform.rotation, transform.parent);
            o.Spawn();
        }

        Destroy(gameObject);
    }
}
