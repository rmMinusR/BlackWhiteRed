using System.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NameDisplayPlate))]
public sealed class NameDisplayPlateDriver : NetworkBehaviour
{
    [SerializeField] private PlayerController player;

    private void Awake()
    {
        MatchManager.onMatchStart -= SetName;
        MatchManager.onMatchStart += SetName;

        //TODO update on team reassignment
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        MatchManager.onMatchStart -= SetName;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        //In case players join midgame
        //Delay by a frame to ensure PlayerController's OnNetworkSpawn() runs first
        StartCoroutine(DelayedSetName());
    }

    private IEnumerator DelayedSetName()
    {
        yield return null;
        SetName();
    }

    private void SetName()
    {
        GetComponent<NameDisplayPlate>().Write(player);
    }
}
