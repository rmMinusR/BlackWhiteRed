using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class PlayerMelee : NetworkBehaviour
{
    [SerializeField]
    float distance;
    [SerializeField]
    LayerMask playerAndGroundLayer;
    [SerializeField]
    LayerMask groundLayer;

    PlayerController playerController;

    PlayerFightingInput input;

    private void Awake()
    {
        input = new PlayerFightingInput();
    }

    private void Start()
    {
        playerController = GetComponent<PlayerController>();
    }

    private void OnEnable()
    {
        input.Enable();
        input.Combat.Enable();
        input.Combat.Melee.Enable();
        input.Combat.Melee.started += ctx => AttemptMelee();
    }

    private void OnDisable()
    {
        input.Disable();
        input.Combat.Disable();
        input.Combat.Melee.Disable();
        input.Combat.Melee.started -= ctx => AttemptMelee();
    }

    private void AttemptMelee()
    {
        //Verify that this is the player's character
        if(gameObject != NetworkManager.Singleton.LocalClient.PlayerObject.gameObject)
        {
            return;
        }
        Debug.Log(name + "is doing Melee");

        //TODO: event invokes for animations, sounds. 
        //TODO: verify the player is holding the melee weapon, and that there's no cooldown
        //TODO: better means for getting the direction the spherecast should go.
        Vector3 directionFacing = Camera.main.transform.forward;

        //Hit Registration
        RaycastHit[] hits;
        hits = Physics.RaycastAll(new Ray(transform.position, directionFacing), distance, playerAndGroundLayer);

        if(hits.Length > 0)
        {
            PlayerController playerHit = null;
            PlayerController tempPlayer = null;
            float dist = Mathf.Infinity;
            float tempDist;
            bool tempCheck;

            for (int i = 0; i < hits.Length; i++)
            {
                tempCheck = hits[i].transform.TryGetComponent<PlayerController>(out tempPlayer);
                if(!tempCheck || tempPlayer.Team != playerController.Team)
                {
                    tempDist = hits[i].distance;
                    if(dist > tempDist)
                    {
                        dist = tempDist;
                        playerHit = tempPlayer;
                    }
                }
            }

            if (playerHit != null)
            {
                Debug.Log("Melee hits locally!");
                MeleeCheckServerRpc(directionFacing, dist, playerHit.NetworkObjectId);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void MeleeCheckServerRpc(Vector3 directionFacing, float castDistance, ulong playerHitId)
    {
        PlayerController playerHit = NetworkManager.Singleton.SpawnManager.SpawnedObjects[playerHitId].GetComponent<PlayerController>();

        //Verify the hit was not blocked by something server-side
        if (!Physics.Raycast(new Ray(transform.position, directionFacing), castDistance, groundLayer))
        {
            Debug.Log("Melee hits server-side!");
            //TODO: Create something that deals with protection in PlayerHealth, then call it here instead
            playerHit.GetComponent<PlayerHealth>().TakeDamage((int)playerController.CurrentStats.damageDealt);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Camera.main.transform.forward * distance);
    }
}
