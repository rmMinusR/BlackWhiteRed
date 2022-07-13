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
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            return;
        }

        Debug.LogError("ArrowPool Instance already exists, deleting " + this.name);
        Destroy(this);
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
            GameObject obj = Instantiate(prefab,transform);
            obj.SetActive(false);
            inactive.Add(obj);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestArrowFireServerRpc(Team team, int shadeValue, Vector3 startingPosition, Vector3 startDirection, float amountCharged)
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
        obj.GetComponent<ArrowController>().Init(team, shadeValue, startingPosition, startDirection, amountCharged);

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
