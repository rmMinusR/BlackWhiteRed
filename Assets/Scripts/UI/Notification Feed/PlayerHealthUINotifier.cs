using System.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(PlayerHealth))]
public class PlayerHealthUINotifier : NetworkBehaviour
{
    private PlayerHealth src;
    private CharacterKinematics kinematics;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        src = GetComponent<PlayerHealth>();
        kinematics = GetComponent<CharacterKinematics>();

        if (IsServer)
        {
            src.onHealthChange -= RecordDamager;
            src.onHealthChange += RecordDamager;

            src.onPlayerDeath -= OnDeath;
            src.onPlayerDeath += OnDeath;
        }
    }

    private void OnDisable()
    {
        src.onHealthChange -= RecordDamager;
        src.onPlayerDeath -= OnDeath;
    }

    private (DamageSource, PlayerController)? recordedLastDamage; //Cleared when touching ground
    private void RecordDamager(int newHP, DamageSource damageSource, PlayerController damager)
    {
        recordedLastDamage = (damageSource, damager);
    }

    private void FixedUpdate()
    {
        if (kinematics.frame.isGrounded) recordedLastDamage = null;
    }

    private void OnDeath(DamageSource damageSource, PlayerController killer)
    {
        if (recordedLastDamage.HasValue && damageSource.HasFlag(DamageSource.ABYSS) && killer == null)
        {
            damageSource |= recordedLastDamage.Value.Item1;
            killer = recordedLastDamage.Value.Item2;
        }
        
        NotificationFeed.Instance.BroadcastDeathMessage(killer, src.GetComponent<PlayerController>(), damageSource);
    }
}
