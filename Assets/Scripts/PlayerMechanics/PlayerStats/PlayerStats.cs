using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Player Stats", menuName = "Player Stats")]
public class PlayerStats : ScriptableObject
{
    //Armor
    [Header("Armor")]
    [SerializeField]
    public float armorStrength;

    //Movement
    [Space]
    [Header("Movement")]
    [SerializeField]
    public float speedMultiplier;
    [SerializeField]
    public float jumpMultiplier;

    //Swordplay
    [Space]
    [Header("Swords")]
    [SerializeField]
    public float damageDealt;
    [SerializeField]
    public float swordKnockbackMultiplier;

    //Archery
    //TODO: Make the archery stats not pop up in inspector if armedWithBow is set to false
    [Space]
    [Header("Archery")]
    [SerializeField]
    public bool armedWithBow;
    [SerializeField]
    public float damageMultiplier;
    [SerializeField]
    public float bowKnockbackMultiplier;
}
