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
    LayerMask playerLayer;

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
        Vector3 directionFacing = transform.forward;

        //Hit Registration
        RaycastHit[] hits;
        hits = Physics.RaycastAll(new Ray(transform.position, directionFacing), distance, playerLayer);


        if(hits.Length > 0)
        {
            PlayerController playerHit = null;
            PlayerController tempPlayer;
            float dist = Mathf.Infinity;
            float tempDist;
            bool tempCheck;

            for (int i = 0; i < hits.Length; i++)
            {
                tempCheck = hits[i].rigidbody.TryGetComponent<PlayerController>(out tempPlayer);
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
                Debug.Log("Melee hits!");
                MeleeCheckServerRpc(directionFacing, playerHit.NetworkObjectId);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void MeleeCheckServerRpc(Vector3 directionFacing, ulong playerHitId)
    {
        PlayerController playerHit = NetworkManager.Singleton.SpawnManager.SpawnedObjects[playerHitId].GetComponent<PlayerController>();

        playerHit.GetComponent<PlayerHealth>().TakeDamage((int)playerController.CurrentStats.damageDealt);
        ////Verify the hit was possible in range
        //if (Vector3.SqrMagnitude(playerHit.transform.position - transform.position) <= distance * distance)
        //{ 
        //    //TODO: Create something that deals with protection in PlayerHealth, then call it here instead
        //    playerHit.GetComponent<PlayerHealth>().TakeDamage((int)playerController.CurrentStats.damageDealt);
        //}
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * distance);
    }
}
