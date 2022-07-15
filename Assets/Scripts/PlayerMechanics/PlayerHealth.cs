using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerHealth : NetworkBehaviour
{

    const int MAX_HEALTH = 20;

    PlayerController playerController;

    [SerializeField]
    [InspectorReadOnly]
    private NetworkVariable<int> health = new NetworkVariable<int>(default,NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Owner);

    public delegate void TriggerEvent();
    public event TriggerEvent onPlayerDeath;

    public delegate void IntEvent(int _value);
    public event IntEvent onHealthChange;

    void Start()
    {
        health.Value = MAX_HEALTH;
        playerController = GetComponent<PlayerController>();
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

    public void TakeDamage(int damage)
    {
        health.Value = Mathf.Max(health.Value - damage,0);
        health.SetDirty(true);

        onHealthChange?.Invoke(health.Value);

        if (health.Value == 0)
        {
            onPlayerDeath?.Invoke();
        }
    }

    private void OnHealthChange(int oldValue, int newValue)
    {
        onHealthChange?.Invoke(newValue);

        if (newValue == 0)
        {
            onPlayerDeath?.Invoke();
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
