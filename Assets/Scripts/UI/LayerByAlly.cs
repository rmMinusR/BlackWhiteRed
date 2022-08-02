using System.Collections;
using Unity.Netcode;
using UnityEngine;

public sealed class LayerByAlly : NetworkBehaviour
{
    [SerializeField] private PlayerController playerController;
    [SerializeField] [Min(0)] private int ifAlly;
    [SerializeField] [Min(0)] private int ifEnemy;

    private void Update()
    {
        if(IsSpawned)
        {
            //TODO call only on team assignment (and on spawn)

            int targetLayer = (NetHeartbeat.Self.GetComponent<PlayerController>().CurrentTeam == playerController.CurrentTeam)
                ? ifAlly
                : ifEnemy;

            if (gameObject.layer != targetLayer) gameObject.layer = targetLayer;
        }
    }
}
