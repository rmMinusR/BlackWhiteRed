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

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        //In case players join midgame
        //Delay by a frame to ensure PlayerController's OnNetworkSpawn() runs first
        StartCoroutine(DelayedUpdateLayer());
    }

    private IEnumerator DelayedUpdateLayer()
    {
        yield return null;
        UpdateLayer();
    }

    private void UpdateLayer()
    {
        int targetLayer = (NetHeartbeat.Self.GetComponent<PlayerController>().CurrentTeam == playerController.CurrentTeam)
                ? ifAlly
                : ifEnemy;

        gameObject.layer = targetLayer;
    }
}
