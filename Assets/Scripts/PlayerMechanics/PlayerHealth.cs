using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;

[Flags]
public enum DamageSource
{
    INVALID = 0,

    SWORD     = 1 << 0,
    ARROW     = 1 << 1,

    ABYSS     = 1 << 8,
    EXPLOSION = 1 << 9
}

public class PlayerHealth : NetworkBehaviour
{

    const int MAX_HEALTH = 20;
    const float PERCENTAGE_PROTECTION = .15f;

    PlayerController playerController;

    private NetworkVariable<int> health = new NetworkVariable<int>(MAX_HEALTH, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public event Action<     DamageSource, PlayerController> clientside_onPlayerDeath;
    public event Action<int, DamageSource, PlayerController> clientside_onHealthChange;

    public event Action<     DamageSource, PlayerController> serverside_onPlayerDeath;
    public event Action<int, DamageSource, PlayerController> serverside_onHealthChange;

    void Start()
    {
        playerController = GetComponent<PlayerController>();
    }

    private void Update()
    {
        CheckForVoid();
    }

    private void CheckForVoid()
    {
        if (IsHost || IsServer)
        {
            if (transform.position.y < -15)
            {
                TakeDamage(1000, DamageSource.ABYSS);
            }
        }
    }

    public void TakeDamage(float attackDamage, DamageSource damageSource, PlayerController attacker = null)
    {
        //RSC: Block clients from calling this
        if (!IsServer) throw new AccessViolationException(nameof(TakeDamage)+" is only callable by server!");

        //Account for armor lessening damage
        int damage = Mathf.CeilToInt(attackDamage * (1 - PERCENTAGE_PROTECTION * playerController.CurrentStats.armorStrength));

        TakeDamageFlat(damage, damageSource, attacker);
    }

    private void TakeDamageFlat(int damage, DamageSource damageSource = DamageSource.INVALID, PlayerController attacker = null)
    {
        //RSC: Block clients from calling this
        if (!IsServer) throw new AccessViolationException(nameof(TakeDamageFlat)+" is only callable by server!");

        health.Value = Mathf.Max(health.Value - damage, 0);

        //Fire callbacks
        Debug.Log("Damaging player (clientside) - new HP="+health.Value);
        serverside_onHealthChange?.Invoke(health.Value, damageSource, attacker);
        ChangeHealthClientRpc(health.Value, -damage, damageSource, attacker, ClientIDCache.Narrowcast(OwnerClientId)); //Should we broadcast instead?
        
        //If we're out of health, die
        if (health.Value <= 0) Kill();
    }

    [ClientRpc]
    private void ChangeHealthClientRpc(int newHealth, int delta, DamageSource deltaSource, PlayerController deltaActor, ClientRpcParams p)
    {
        Debug.Log("Damaging player (clientside)");
        clientside_onHealthChange?.Invoke(newHealth, deltaSource, deltaActor);
    }

    private void Kill(DamageSource damageSource = DamageSource.INVALID, PlayerController attacker = null)
    {
        //RSC: Block clients from calling this
        if (!IsServer) throw new AccessViolationException(nameof(Kill)+" is only callable by server!");
        
        //Fire callbacks
        Debug.Log("Killing player (serverside)", this);
        serverside_onPlayerDeath?.Invoke(damageSource, attacker);
        OnDeathClientRpc(damageSource, attacker, default); //Broadcast

        //This call could be delayed in future
        OnRespawn();
    }

    [ClientRpc]
    private void OnDeathClientRpc(DamageSource finalBlow, PlayerController finalBlowDealer, ClientRpcParams p)
    {
        Debug.Log("Killing player (clientside)", this);
        clientside_onPlayerDeath?.Invoke(finalBlow, finalBlowDealer);
    }

    private void OnRespawn()
    {
        //RSC: Block clients from calling this
        if (!IsServer) throw new AccessViolationException(nameof(OnRespawn)+" is only callable by server!");

        playerController.ResetToSpawnPoint();
        health.Value = MAX_HEALTH;
    }

    public int GetMaxHealth()
    {
        return MAX_HEALTH;
    }

    public int GetCurrentHealth()
    {
        return health.Value;
    }

}
