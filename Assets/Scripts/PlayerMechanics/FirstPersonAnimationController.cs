using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirstPersonAnimationController : MonoBehaviour
{
    Animator animator;

    [SerializeField]
    PlayerWeaponHolding playerWeaponHolding;
    [SerializeField]
    PlayerBow playerBow;
    [SerializeField]
    PlayerMelee playerMelee;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnEnable()
    {
        MatchManager.onMatchStart += HandleMatchStart;
        EnablePlayerScripts();
    }

    private void EnablePlayerScripts()
    {
        if (playerWeaponHolding != null)
        {
            playerWeaponHolding.onWeaponChange += HandleWeaponChange;
        }

        if (playerMelee != null)
        {
            playerMelee.onSwing += HandleSwordSwing;
        }

        if (playerBow != null)
        {
            playerBow.onChargingChange += HandleBowCharging;
        }
    }

    private void DisablePlayerScripts()
    {
        if (playerWeaponHolding != null)
        {
            playerWeaponHolding.onWeaponChange -= HandleWeaponChange;
        }

        if (playerMelee != null)
        {
            playerMelee.onSwing -= HandleSwordSwing;
        }

        if (playerBow != null)
        {
            playerBow.onChargingChange -= HandleBowCharging;
        }
    }

    private void HandleBowCharging(bool _value)
    {
        animator.SetBool("BowPull", _value);
    }

    private void HandleSwordSwing()
    {
        animator.SetTrigger("SwordSwing");
    }

    private void HandleWeaponChange(WeaponHeld weaponHeld)
    {
        animator.SetBool("Weapon", weaponHeld == WeaponHeld.BOW);
    }

    private void HandleMatchStart()
    {
        playerWeaponHolding = MatchManager.Instance.localPlayerController.GetComponent<PlayerWeaponHolding>();
        playerMelee = MatchManager.Instance.localPlayerController.GetComponent<PlayerMelee>();
        playerBow = MatchManager.Instance.localPlayerController.GetComponent<PlayerBow>();

        EnablePlayerScripts();
    }

    private void OnDisable()
    {
        MatchManager.onMatchStart -= HandleMatchStart;
        DisablePlayerScripts();
    }


}
