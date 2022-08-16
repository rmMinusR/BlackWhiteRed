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

    private void SetName()
    {
        if (!IsSpawned) return;
        GetComponent<NameDisplayPlate>().Write(player);
    }
}
