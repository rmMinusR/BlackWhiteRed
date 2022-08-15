using System.Collections;
using Unity.Netcode;
using UnityEngine;

public sealed class LayerByAlly : NetworkBehaviour
{
    [SerializeField] private PlayerController playerController;
    [SerializeField] [Min(0)] private int ifAlly;
    [SerializeField] [Min(0)] private int ifEnemy;

    private void Awake()
    {
        MatchManager.onMatchStart -= UpdateLayer;
        MatchManager.onMatchStart += UpdateLayer;

        //TODO update on team reassignment
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        MatchManager.onMatchStart -= UpdateLayer;
    }

    private void UpdateLayer()
    {
        if (!IsSpawned) return;

        int targetLayer = (NetHeartbeat.Self.GetComponent<PlayerController>().CurrentTeam == playerController.CurrentTeam)
                ? ifAlly
                : ifEnemy;

        gameObject.layer = targetLayer;
    }
}
