using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public enum WeaponHeld
{
    SWORD,
    BOW
}

public class PlayerWeaponHolding : NetworkBehaviour
{
    public static float scrollThreshold = 60.0f;
    [SerializeField]
    [Min(0.01f)]
    float scrollResetTime = 0.1f;
    float scrollAmount;
    float scrollTimer;

    PlayerController playerController;
    PlayerFightingInput input;

    [SerializeField]
    bool isLocalPlayer = false;

    [SerializeField]
    private NetworkVariable<WeaponHeld> holding = new NetworkVariable<WeaponHeld>(WeaponHeld.SWORD,NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Owner);

    public delegate void WeaponHeldEvent(WeaponHeld weaponHeld);
    public event WeaponHeldEvent onWeaponChange;

    private void Awake()
    {
        input = new PlayerFightingInput();
        playerController = GetComponent<PlayerController>();
    }

    private void OnEnable()
    {
        input.Enable();
        input.Combat.Enable();
        input.Combat.Melee.Enable();
        input.Combat.ChangeWeapon.started += ctx => HandleScroll(ctx.ReadValue<float>());

        playerController.onShadeChange += HandleShadeChange;

        holding.OnValueChanged += HandleHoldingValueChanged;

        MatchManager.onMatchStart += HandleMatchStart;
    }

    private void Update()
    {
        if (scrollTimer > 0)
        {
            scrollTimer -= Time.deltaTime;
        }
    }

    private void HandleHoldingValueChanged(WeaponHeld previousValue, WeaponHeld newValue)
    {
        switch (newValue)
        {
            case WeaponHeld.SWORD:
                break;
            case WeaponHeld.BOW:
                break;
        }
    }

    private void HandleMatchStart()
    {
        Debug.Log("PlayerWeaponHolding HandleMatchStart");


        isLocalPlayer = gameObject == NetworkManager.Singleton.LocalClient.PlayerObject.gameObject;
    }

    private void HandleShadeChange(PlayerStats _value)
    {
        if (isLocalPlayer)
        {
            WeaponHeld oldHolding = holding.Value;

            if (!_value.armedWithBow)
            {
                holding.Value = WeaponHeld.SWORD;
            }

            if(oldHolding != holding.Value)
            {
                onWeaponChange?.Invoke(holding.Value);
            }
        }
    }

    private void HandleScroll(float delta)
    {
        if (isLocalPlayer)
        {
            scrollAmount += delta;

            if (scrollTimer > 0)
            {
                while (scrollAmount > scrollThreshold)
                {
                    scrollAmount -= scrollThreshold;
                    ChangeHolding();
                }

                while (scrollAmount < -scrollThreshold)
                {
                    scrollAmount += scrollThreshold;
                    ChangeHolding();
                }
            }
            else if(delta != 0)
            {
                ChangeHolding();
                scrollAmount = 0;
            }

            scrollTimer = scrollResetTime;
        }
    }

    private void ChangeHolding()
    {
        //Verify the player can actually change their item
        if(!playerController.CurrentStats.armedWithBow)
        {
            return;
        }

        holding.Value = (WeaponHeld)(((int)holding.Value + 1) % 2);

        switch (holding.Value)
        {
            case WeaponHeld.SWORD:
                break;
            case WeaponHeld.BOW:
                break;
        }

        onWeaponChange?.Invoke(holding.Value);
    }

    public bool CanPreform(WeaponHeld held)
    {
        return isLocalPlayer && holding.Value == held;
    }
}