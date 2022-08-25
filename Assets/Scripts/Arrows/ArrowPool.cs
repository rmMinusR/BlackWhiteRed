using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ArrowPool : NetworkBehaviour
{
    [SerializeField]
    GameObject prefab;
    [SerializeField]
    [Min(1)]
    int count;

    [SerializeField]
    List<GameObject> inactive;
    [SerializeField]
    List<GameObject> active;

    public static ArrowPool Instance;
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (Instance == null)
        {
            Instance = this;
            Init();
        }
        else
        {
            Debug.LogError($"{nameof(ArrowPool)}.{nameof(Instance)} already exists, deleting {name}");
            NetworkObject.Despawn();
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        MatchManager.onMatchStart += Init;
    }

    private void OnDisable()
    {
        MatchManager.onMatchStart -= Init;
    }

    private void Init()
    {
        if(!(NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost))
        {
            return;
        }

        inactive = new List<GameObject>();
        active = new List<GameObject>();

        for (int i = 0; i < count; i++)
        {
            GameObject obj = Instantiate(prefab);
            obj.GetComponent<NetworkObject>().Spawn(true);
            obj.GetComponent<NetworkObject>().TrySetParent(gameObject);
            obj.GetComponent<NetworkObject>().Despawn(false);
            obj.SetActive(false);
            inactive.Add(obj);
        }
    }

    [ServerRpc(Delivery = RpcDelivery.Reliable, RequireOwnership = false)]
    public void RequestArrowFireServerRpc(Team team, ulong shooterId, int shadeValue, Vector3 startingPosition, Vector3 startDirection, float amountCharged, float timeShot)
    {
        Debug.Log("Requested Arrow Fire");

        if(inactive.Count == 0)
        {
            inactive.Add(active[0]);
            active.RemoveAt(0);
        }
        GameObject obj = inactive[0];
        obj.SetActive(true);
        if (!obj.GetComponent<NetworkObject>().IsSpawned)
        {
            obj.GetComponent<NetworkObject>().Spawn(true);
        }
        obj.SetActive(true);
        obj.GetComponent<ArrowController>().InitClientRpc(team, shooterId, shadeValue, startingPosition, startDirection, amountCharged, timeShot);

        inactive.RemoveAt(0);
        active.Add(obj);
    }

    public void UnloadArrow(GameObject arrow)
    {
        if(active.Contains(arrow))
        {
            active.Remove(arrow);
            inactive.Add(arrow);
            arrow.SetActive(false);
            if (arrow.GetComponent<NetworkObject>().IsSpawned)
            {
                arrow.GetComponent<NetworkObject>().Despawn(false);
            }
        }
    }
}
