using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public enum TeamSoundType
{
    SWORD_SWING,
    SWORD_STAB,
    BOW_PULL,
    BOW_SHOOT,
    BUFF_SHADE,
    DEBUFF_SHADE,
    YOU_DIED,
    SOMEONE_DIED,
    SHOT_LANDED,
    SCORE_STINGER,
    MOVEMENT
}

[System.Serializable]
public struct TeamDependentSoundEventReference
{
    public TeamSoundType type;
    public FMODUnity.EventReference allyVersion;
    public FMODUnity.EventReference enemyVersion;
}

public class PlayerSoundEmitter : NetworkBehaviour
{
    [SerializeField]
    TeamDependentSoundEventReference[] teamDependentSoundsArray;

    Dictionary<TeamSoundType, TeamDependentSoundEventReference> teamDependentSounds;

    //Kinematics Movement Params
    [SerializeField]
    float minimumMovementSoundSpeed;
    [SerializeField]
    float slowWalkingTimeBetweenSteps;
    [SerializeField]
    float walkingMovementSoundSpeed;
    [SerializeField]
    float walkingTimeBetweenSteps;
    [SerializeField]
    float runningMovementSoundSpeed;
    [SerializeField]
    float runningTimeBetweenSteps;
    [SerializeField]
    float movementSoundTimer;

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
    CharacterKinematics kinematics;

    //Bow Charging
    FMOD.Studio.EventInstance bowDrawEventInstance;

    private void Awake()
    {
        teamDependentSounds = new Dictionary<TeamSoundType, TeamDependentSoundEventReference>();
        for(int i = 0; i < teamDependentSoundsArray.Length; i++)
        {
            teamDependentSounds.Add(teamDependentSoundsArray[i].type, teamDependentSoundsArray[i]);
        }
    }

    private void OnEnable()
    {
        MatchManager.onMatchStart += HandleMatchStart;
        MatchManager.onTeamScore += HandleTeamScore;
        EnablePlayerScripts();
    }

    private void OnDisable()
    {
        MatchManager.onMatchStart -= HandleMatchStart;
        MatchManager.onTeamScore -= HandleTeamScore;
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

        if (playerHealth != null)
        {
            //Can't figure out what's up with the death sounds not sending >:/
            playerHealth.serverside_onPlayerDeath += HandlePlayerDeathServer;
            playerHealth.clientside_onPlayerDeath += HandlePlayerDeathClient;
            playerHealth.serverside_onHealthChange += HandlePlayerDamaged;
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
        playerHealth = GetComponent<PlayerHealth>();
        kinematics = GetComponent<CharacterKinematics>();

        //Data
        isLocal = playerController == MatchManager.Instance.localPlayerController;
        isAlly = playerController.CurrentTeam == MatchManager.Instance.localPlayerController.CurrentTeam;
        lastShadeValue = playerController.ShadeValue;

        //Correct bow draw event instance
        TeamDependentSoundEventReference teamSoundEvent = teamDependentSounds[TeamSoundType.BOW_PULL];
        bowDrawEventInstance = FMODUnity.RuntimeManager.CreateInstance(isAlly ? teamSoundEvent.allyVersion : teamSoundEvent.enemyVersion);

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
                CallForTeamSound(TeamSoundType.DEBUFF_SHADE);
            }
            else if(lastShadeValue - newShadeValue == 1)
            {
                CallForTeamSound(TeamSoundType.BUFF_SHADE);
            }
        }
        lastShadeValue = newShadeValue;
    }

    private void HandleBowCharging(bool _value)
    {
        if (!IsOwner)
        {
            return;
        }

        ControlBowDraw(_value);
        BowDrawSettingServerRpc(_value);
    }

    private void HandleSwordSwing()
    {
        if(!IsOwner)
        {
            return;
        }

        CallForTeamSound(TeamSoundType.SWORD_SWING);
        SoundForOthersServerRpc(TeamSoundType.SWORD_SWING);
    }

    private void HandlePlayerDamaged(int arg1, DamageSource arg2, PlayerController arg3)
    {
        if (arg2 == DamageSource.ARROW)
        {
            arg3.GetComponent<PlayerSoundEmitter>().SoundForSelfClientRpc(TeamSoundType.SHOT_LANDED);
        }
        else if(arg2 == DamageSource.SWORD)
        {
            SoundForAllClientRpc(TeamSoundType.SWORD_STAB);
        }
    }

    private void HandlePlayerDeathServer(DamageSource arg1, PlayerController arg2)
    {
        SoundForOthersClientRpc(TeamSoundType.SOMEONE_DIED);
        SoundForAllClientRpc(TeamSoundType.SOMEONE_DIED);
    }

    public void HandlePlayerDeathClient(DamageSource arg1, PlayerController arg2)
    {
        if (isLocal)
        {
            CallForTeamSound(TeamSoundType.YOU_DIED);
        }
        else
        {
            CallForTeamSound(TeamSoundType.SOMEONE_DIED);
        }
    }

    private void HandleTeamScore(Team team)
    {
        if (isLocal)
        {
            TeamDependentSoundEventReference teamSoundEvent = teamDependentSounds[TeamSoundType.SCORE_STINGER];
            SpatializedSoundSystem.Instance.PlayReleasedSpatializedSound(
                (team == playerController.CurrentTeam ? teamSoundEvent.allyVersion : teamSoundEvent.enemyVersion),
                transform.position
                );
        }
    }

    [ServerRpc(Delivery = RpcDelivery.Reliable, RequireOwnership = true)]
    private void BowDrawSettingServerRpc(bool _value)
    {
        BowDrawSettingClientRpc(_value);
    }

    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    private void BowDrawSettingClientRpc(bool _value)
    {
        if (IsOwner)
        {
            return;
        }

        ControlBowDraw(_value);
    }

    [ServerRpc(Delivery = RpcDelivery.Reliable,RequireOwnership = true)]
    public void SoundForOthersServerRpc(TeamSoundType type)
    {
        SoundForOthersClientRpc(type);
    }

    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    public void SoundForOthersClientRpc(TeamSoundType type)
    {
        if (!isLocal)
        {
            CallForTeamSound(type);
        }
    }

    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    public void SoundForSelfClientRpc(TeamSoundType type)
    {
        if (isLocal)
        {
            CallForTeamSound(type);
        }
    }

    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    public void SoundForAllClientRpc(TeamSoundType type)
    {
        CallForTeamSound(type);
    }

    public void CallForTeamSound(TeamSoundType type)
    {
        if (teamDependentSounds.ContainsKey(type))
        {
            TeamDependentSoundEventReference teamSoundEvent = teamDependentSounds[type];
            SpatializedSoundSystem.Instance.PlayReleasedSpatializedSoundAttached(
                (isAlly ? teamSoundEvent.allyVersion : teamSoundEvent.enemyVersion),
                transform
                );
        }
    }

    private void ControlBowDraw(bool _value)
    {
        if (_value)
        {
            bowDrawEventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            FMODUnity.RuntimeManager.AttachInstanceToGameObject(bowDrawEventInstance, transform);
            bowDrawEventInstance.start();
        }
        else
        {
            bowDrawEventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        }
    }

    private void Update()
    {
        if(kinematics != null)
        {
            if (kinematics.frame.isGrounded && kinematics.frame.velocity.sqrMagnitude > minimumMovementSoundSpeed * minimumMovementSoundSpeed)
            {
                movementSoundTimer += Time.deltaTime;
                if (kinematics.frame.velocity.sqrMagnitude > runningMovementSoundSpeed * runningMovementSoundSpeed
                    && movementSoundTimer > runningTimeBetweenSteps)
                {
                    movementSoundTimer -= runningTimeBetweenSteps;
                    PlayMovementSound(2);
                }
                else if (kinematics.frame.velocity.sqrMagnitude > walkingMovementSoundSpeed * walkingMovementSoundSpeed
                    && movementSoundTimer > walkingMovementSoundSpeed)
                {
                    PlayMovementSound(0);
                    movementSoundTimer -= walkingTimeBetweenSteps;
                }
                else if (movementSoundTimer > slowWalkingTimeBetweenSteps)
                {
                    PlayMovementSound(1);
                    movementSoundTimer -= slowWalkingTimeBetweenSteps;
                }
            }
            else
            {
                movementSoundTimer = 0;
            }
        }
    }

    private void PlayMovementSound(int playerLayer)
    {
        if (teamDependentSounds.ContainsKey(TeamSoundType.MOVEMENT))
        {
            TeamDependentSoundEventReference teamSoundEvent = teamDependentSounds[TeamSoundType.MOVEMENT];
            FMOD.Studio.EventInstance instance = SpatializedSoundSystem.Instance.PlayTrackedSpatializedSound(
                (isAlly ? teamSoundEvent.allyVersion : teamSoundEvent.enemyVersion),
                transform.position
                );
            instance.setParameterByName("PlayerLayer",playerLayer);
            instance.release();
        }
    }
}