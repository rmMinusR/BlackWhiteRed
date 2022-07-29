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

    public event Action<     DamageSource, PlayerController> onPlayerDeath;
    
    public event Action<int, DamageSource, PlayerController> onHealthChange;

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

    private void OnEnable()
    {
        health.OnValueChanged += OnHealthChange;
        MatchManager.onTeamScore += HandleTeamScore;
        MatchManager.onTeamWin += HandleTeamScore;
    }

    private void OnDisable()
    {
        health.OnValueChanged -= OnHealthChange;
        MatchManager.onTeamScore -= HandleTeamScore;
        MatchManager.onTeamWin -= HandleTeamScore;
    }

    public void TakeDamage(float attackDamage, DamageSource damageSource, PlayerController attacker = null)
    {
        //Account for armor lessening damage
        int damage = Mathf.CeilToInt(attackDamage * (1 - PERCENTAGE_PROTECTION * playerController.CurrentStats.armorStrength));

        TakeDamageFlat(damage, damageSource, attacker);
    }

    private void TakeDamageFlat(int damage, DamageSource damageSource = DamageSource.INVALID, PlayerController attacker = null)
    {
        health.Value = Mathf.Max(health.Value - damage,0);
        health.SetDirty(true);

        onHealthChange?.Invoke(health.Value, damageSource, attacker);

        if (health.Value == 0)
        {
            onPlayerDeath?.Invoke(damageSource, attacker);
            playerController.ResetToSpawnPoint();
            HandleTeamScore(Team.INVALID);
        }
    }

    private void OnHealthChange(int oldValue, int newValue)
    {
        //RSC: Clientside - Can't easily access player who hit, so use null
        onHealthChange?.Invoke(newValue, DamageSource.INVALID, null);

        if (newValue == 0)
        {
            onPlayerDeath?.Invoke(DamageSource.INVALID, null);
            playerController.ResetToSpawnPoint();
            HandleTeamScore(Team.INVALID);
        }
    }

    private void HandleTeamScore(Team team)
    {
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
