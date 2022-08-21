using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class FirstPersonAnimationController : MonoBehaviour
{
    [SerializeField]
    SkinnedMeshRenderer bowRenderer;
    [SerializeField]
    int bowIndex;
    [SerializeField]
    MeshRenderer swordRenderer;
    [SerializeField]
    int swordIndex;

    Animator animator;

    PlayerController playerController;
    PlayerWeaponHolding playerWeaponHolding;
    PlayerBow playerBow;
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

        if (playerController != null)
        {
            playerController.onShadeChange += HandleShadeChange;
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

        if (playerController != null)
        {
            playerController.onShadeChange -= HandleShadeChange;
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

    private void HandleShadeChange(PlayerStats _value)
    {
        int shadeValue = playerController.ShadeValue;

        Material[] tempMaterials;
        //Bow
        tempMaterials = bowRenderer.materials;
        tempMaterials[bowIndex].SetFloat("_Power", 1 - shadeValue / 3.0f);
        bowRenderer.materials = tempMaterials;
        //Sword
        tempMaterials = swordRenderer.materials;
        tempMaterials[swordIndex].SetFloat("_Power", 1 - shadeValue / 6.0f);
        swordRenderer.materials = tempMaterials;
    }

    private void HandleMatchStart()
    {
        playerController = MatchManager.Instance.localPlayerController;
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
