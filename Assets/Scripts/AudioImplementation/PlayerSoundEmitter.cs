using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[System.Serializable]
public struct TeamDependentSoundEventReference
{
    public FMODUnity.EventReference allyVersion;
    public FMODUnity.EventReference enemyVersion;
}

public class PlayerSoundEmitter : NetworkBehaviour
{
    [SerializeField]
    TeamDependentSoundEventReference swordSwing;
    [SerializeField]
    TeamDependentSoundEventReference swordStab;
    [SerializeField]
    TeamDependentSoundEventReference bowPull;
    [SerializeField]
    TeamDependentSoundEventReference bowShoot;
    [SerializeField]
    TeamDependentSoundEventReference buffShade;
    [SerializeField]
    TeamDependentSoundEventReference debuffShade;

    //LogicData
    bool isLocal;
    bool isAlly;
    int lastShadeValue = 0;

    //Components
    PlayerController playerController;
    PlayerWeaponHolding playerWeaponHolding;
    PlayerBow playerBow;
    PlayerMelee playerMelee;
    PlayerHealth playerHealth;

    private void OnEnable()
    {
        MatchManager.onMatchStart += HandleMatchStart;
        EnablePlayerScripts();
    }

    private void OnDisable()
    {
        MatchManager.onMatchStart -= HandleMatchStart;
        DisablePlayerScripts();
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

    private void HandleMatchStart()
    {
        //Components set-up
        playerController = GetComponent<PlayerController>();
        playerWeaponHolding = GetComponent<PlayerWeaponHolding>();
        playerMelee = GetComponent<PlayerMelee>();
        playerBow = GetComponent<PlayerBow>();

        //Data
        isLocal = playerController == MatchManager.Instance.localPlayerController;
        isAlly = playerController.CurrentTeam == MatchManager.Instance.localPlayerController.CurrentTeam;
        lastShadeValue = playerController.ShadeValue;

        EnablePlayerScripts();
    }

    private void HandleWeaponChange(WeaponHeld weaponHeld)
    {
        
    }

    private void HandleShadeChange(PlayerStats _value)
    {
        int newShadeValue = playerController.ShadeValue;
        if(isLocal)
        {
            if(newShadeValue > lastShadeValue)
            {
                CallForTeamSound(buffShade);
            }
            else if(lastShadeValue - newShadeValue == 1)
            {
                CallForTeamSound(debuffShade);
            }
        }
        lastShadeValue = newShadeValue;
    }

    private void HandleBowCharging(bool _value)
    {
        
    }

    
    private void HandleSwordSwing()
    {
        if(!IsOwner)
        {
            return;
        }

        CallForTeamSound(swordSwing);
        SwordSwingCServerRpc();
    }

    [ServerRpc(Delivery = RpcDelivery.Reliable,RequireOwnership = true)]
    public void SwordSwingCServerRpc()
    {
        SwordSwingClientRpc();
    }

    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    public void SwordSwingClientRpc()
    {
        if (!isLocal)
        {
            CallForTeamSound(swordSwing);
        }
    }

    private void CallForTeamSound(TeamDependentSoundEventReference teamSoundEvent)
    {
        SpatializedSoundSystem.Instance.PlayReleasedSpatializedSound(
            (isAlly? teamSoundEvent.allyVersion: teamSoundEvent.enemyVersion),
            transform.position
            );
    }
}