﻿using System.Collections;
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

        src.serverside_onHealthChange -= RecordDamager;
        src.serverside_onHealthChange += RecordDamager;

        src.serverside_onPlayerDeath -= ForwardDeathToClientUIs;
        src.serverside_onPlayerDeath += ForwardDeathToClientUIs;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        src.serverside_onHealthChange -= RecordDamager;
        src.serverside_onPlayerDeath -= ForwardDeathToClientUIs;
    }

    private (DamageSource src, PlayerController who)? recordedLastDamage; //Cleared when touching ground
    private void RecordDamager(int newHP, DamageSource damageSource, PlayerController damager)
    {
        Debug.Log("Recording damager", this);
        recordedLastDamage = (damageSource, damager);
    }

    private void FixedUpdate()
    {
        if (kinematics.frame.isGrounded) recordedLastDamage = null;
    }

    private void ForwardDeathToClientUIs(DamageSource damageSource, PlayerController killer)
    {
        Debug.Log("Notifying of death", this);

        if (recordedLastDamage.HasValue && damageSource.HasFlag(DamageSource.ABYSS) && killer == null)
        {
            damageSource |= recordedLastDamage.Value.src;
            killer = recordedLastDamage.Value.who;
        }

        BroadcastDeathMessageToUI_ClientRpc(killer, damageSource);
    }

    [ClientRpc]
    private void BroadcastDeathMessageToUI_ClientRpc(PlayerController killer, DamageSource damageSource, ClientRpcParams p = default) => NotificationFeed.Instance.ShowDeathMessage(killer, src.GetComponent<PlayerController>(), damageSource);
}
