using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerWeaponHolding))]
public class PlayerBow : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Minimum amount of time the player holds a button for an arrow to fire")]
    float timeToLoad;

    [SerializeField]
    [Tooltip("Amount of time the player holds a button to have a fully charged bow")]
    float timeToFullyCharge;

    PlayerController playerController;
    PlayerWeaponHolding weaponHolding;
    PlayerFightingInput input;

    [SerializeField]
    float timeCharging;
    [SerializeField]
    bool wantsToCharge;

    public delegate void BoolEvent(bool _value);
    public event BoolEvent onChargingChange;

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
        input.Combat.BowCharge.Enable();
        input.Combat.BowCharge.started += ctx => BowCharging();
        input.Combat.BowCharge.canceled += ctx => BowRelease();
    }

    private void OnDisable()
    {
        input.Disable();
        input.Combat.Disable();
        input.Combat.BowCharge.Disable();
        input.Combat.BowCharge.started -= ctx => BowCharging();
        input.Combat.BowCharge.canceled -= ctx => BowRelease();
    }

    // Update is called once per frame
    void Update()
    {
        if(wantsToCharge && weaponHolding.CanPreform(WeaponHeld.BOW))
        {
            timeCharging += Time.deltaTime;
        }
        else
        {
            timeCharging = 0;
        }
    }

    private void BowRelease()
    {
        if (wantsToCharge)
        {
            wantsToCharge = false;
            onChargingChange.Invoke(wantsToCharge);

            if (weaponHolding.CanPreform(WeaponHeld.BOW))
            {
                if (timeCharging > timeToLoad)
                {
                    Debug.Log("Released with enough time to fire");

                    //TODO: Spawn Arrow
                    float pullBack = (timeCharging - timeToLoad) / (timeToFullyCharge - timeToLoad);
                    Vector3 pos = Camera.main.transform.position;
                    Vector3 dir = Camera.main.transform.forward;

                    ArrowPool.Instance.RequestArrowFireServerRpc(playerController.Team, playerController.ShadeValue, pos, dir, pullBack);
                }
            }
        }
    }

    private void BowCharging()
    {
        wantsToCharge = true;
        onChargingChange.Invoke(wantsToCharge);
    }
}
