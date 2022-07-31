using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[System.Serializable]
public struct MaterialSetUp
{
    public SkinnedMeshRenderer skinnedMeshRenderer;
    public MeshRenderer meshRenderer;
    public int materialIndex;
    public Material ifBlack;
    public Material ifWhite;
}

public class ThirdPersonAnimationController : NetworkBehaviour
{
    [SerializeField]
    List<MaterialSetUp> materialSetUps;
    [Space]
    [SerializeField]
    PlayerController playerController;
    [SerializeField]
    CharacterKinematics characterKinematics;

    Animator animator;

    [SerializeField]
    PlayerWeaponHolding playerWeaponHolding;
    [SerializeField]
    PlayerBow playerBow;
    [SerializeField]
    PlayerMelee playerMelee;
    [SerializeField]
    PlayerHealth playerHealth;

    [SerializeField]
    bool isOther;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        MatchManager.onMatchStart += HandleMatchStart;

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

        if (playerHealth != null)
        {
            playerHealth.onHealthChange += HandleHealthChange;
        }
    }

    private void OnDisable()
    {
        MatchManager.onMatchStart -= HandleMatchStart;

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

        if (playerHealth != null)
        {
            playerHealth.onHealthChange -= HandleHealthChange;
        }
    }

    private void HandleBowCharging(bool _value)
    {
        //if (!IsOwner)
        //{
        //    return;
        //}

        animator.SetBool("BowPull", _value);
        SetAnimatorBoolServerRpc("BowPull", _value);
    }

    private void HandleSwordSwing()
    {
        //if (!IsOwner)
        //{
        //    return;
        //}

        animator.SetTrigger("SwordSwing");
        SetAnimatorTriggerServerRpc("SwordSwing");
    }

    private void HandleWeaponChange(WeaponHeld weaponHeld)
    {
        //if (!IsOwner)
        //{
        //    return;
        //}

        animator.SetBool("Weapon", weaponHeld == WeaponHeld.BOW);
        SetAnimatorBoolServerRpc("Weapon", weaponHeld == WeaponHeld.BOW);
    }

    [ServerRpc(Delivery = RpcDelivery.Reliable, RequireOwnership = false)]
    private void SetAnimatorBoolServerRpc(string tag, bool value)
    {
        animator.SetBool(tag, value);
    }

    [ServerRpc(Delivery = RpcDelivery.Reliable, RequireOwnership = false)]
    private void SetAnimatorTriggerServerRpc(string tag)
    {
        animator.SetTrigger(tag);
    }

    private void HandleMatchStart()
    {
        SetMaterials();
    }

    private void HandleHealthChange(int _value)
    {
        animator.SetTrigger("Pain");
    }

    private void SetMaterials()
    {
        Team team = playerController.Team;

        isOther = playerController != MatchManager.Instance.localPlayerController;

        foreach(MaterialSetUp e in materialSetUps)
        {
            if (e.meshRenderer != null)
            {
                if (team == Team.BLACK)
                {
                    e.meshRenderer.material = e.ifBlack;
                }
                else
                {
                    e.meshRenderer.material = e.ifWhite;
                }

                e.meshRenderer.enabled = isOther;
            }
            else
            {
                if (team == Team.BLACK)
                {
                    e.skinnedMeshRenderer.material = e.ifBlack;
                }
                else
                {
                    e.skinnedMeshRenderer.material = e.ifWhite;
                }

                e.skinnedMeshRenderer.enabled = isOther;
            }
        }
    }

    private void Update()
    {
        animator.SetBool("IsGrounded", characterKinematics.frame.isGrounded);

        if(characterKinematics.frame.input.jump)
        {
            animator.SetTrigger("Jump");
        }

        //Rotate player to work with look value
        Quaternion q = Quaternion.Euler(new Vector3(0, characterKinematics.frame.look.x, 0));
        transform.rotation = q;

        //TODO: Wait for the animation parameter for head turning

        //Convert Velocity for Pos Z to be facing forward
        Vector3 vel = Quaternion.AngleAxis(-characterKinematics.frame.look.x, Vector3.up) * characterKinematics.frame.velocity;

        animator.SetFloat("VelocityX", vel.x);
        animator.SetFloat("VelocityZ", vel.z);
    }
}
