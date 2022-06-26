using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Player Stats", menuName = "Player Stats")]
public class PlayerStats : ScriptableObject
{
    //Armor
    float armorStrength;

    //Movement
    float speedMultiplier;
    float jumpMultiplier;

    //Swordplay
    int damageDealt;
    float swordKnockbackMultiplier;

    //Archery
    bool armedWithBow;
    float damageMultiplier;
    float bowKnockbackMultiplier;
}
