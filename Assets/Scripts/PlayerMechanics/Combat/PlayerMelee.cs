using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerWeaponHolding))]
public class PlayerMelee : NetworkBehaviour
{
    [SerializeField]
    float distance;
    [SerializeField]
    LayerMask playerAndGroundLayer;
    [SerializeField]
    LayerMask groundLayer;

    PlayerController playerController;
    PlayerWeaponHolding weaponHolding;

    PlayerFightingInput input;

    public delegate void TriggerEvent();
    public event TriggerEvent onSwing;

    private void Awake()
    {
        input = new PlayerFightingInput();
    }

    private void Start()
    {
        playerController = GetComponent<PlayerController>();
        weaponHolding = GetComponent<PlayerWeaponHolding>();
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
        if (!PlayerLookController.cursorLocked)
        {
            return;
        }

        //Verify that this is the player's character
        if (!weaponHolding.CanPreform(WeaponHeld.SWORD))
        {
            return;
        }

        Debug.Log(name + "is doing Melee");
        onSwing?.Invoke();

        //TODO: verify that there's no cooldown

        //TODO: better means for getting the direction the cast should go.
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
                if(!tempCheck || tempPlayer.CurrentTeam != playerController.CurrentTeam)
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
        PlayerHealth playerHit = NetworkManager.Singleton.SpawnManager.SpawnedObjects[playerHitId].GetComponent<PlayerHealth>();
        PlayerKnockbackController playerKnockback = playerHit.GetComponent<PlayerKnockbackController>();

        //Verify the hit was not blocked by something server-side
        if (!Physics.Raycast(new Ray(transform.position, directionFacing), castDistance, groundLayer))
        {
            Debug.Log("Melee hits server-side!");
            playerKnockback.KnockbackPlayer(directionFacing, playerController.CurrentStats.swordKnockbackMultiplier);
            playerHit.TakeDamage(playerController.CurrentStats.damageDealt,DamageSource.SWORD,playerController);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Camera.main.transform.forward * distance);
    }
}
