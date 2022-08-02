using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(TeleportController), typeof(CharacterKinematics))]
public class PlayerKnockbackController : MonoBehaviour
{
    [SerializeField]
    float baseVerticalKnockback;

    [SerializeField]
    float baseHorizontalKnockbackOnLand;
    [SerializeField]
    float baseHorizontalKnockbackInAir;

    TeleportController teleportController;
    CharacterKinematics characterKinematics;

    public void Start()
    {
        teleportController = GetComponent<TeleportController>();
        characterKinematics = GetComponent<CharacterKinematics>();
    }

    public void KnockbackPlayer(Vector3 direction, float multiplier)
    {
        float horizontalKnockback;

        if(characterKinematics.frame.isGrounded)
        {
            horizontalKnockback = baseHorizontalKnockbackOnLand;
        }
        else
        {
            horizontalKnockback = baseHorizontalKnockbackInAir;
        }

        Vector3 newVelocity = direction;
        newVelocity.y = 0;
        newVelocity = newVelocity.normalized * horizontalKnockback * multiplier;

        newVelocity.y = baseVerticalKnockback;

        teleportController.Teleport(vel: newVelocity);
    }
}
