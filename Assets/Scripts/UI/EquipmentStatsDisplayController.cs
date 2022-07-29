using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class EquipmentStatsDisplayController : MonoBehaviour
{
    [SerializeField]
    float selectedOutlineThickness;
    [SerializeField]
    float unselectedOutlineThickness;
    [SerializeField]
    Color selectedOutlineColor;
    [SerializeField]
    Color unselectedOutlineColor;
    
    [Space]
    [SerializeField]
    GameObject bowPanel;
    [SerializeField]
    Outline[] bowOutlines;
    [SerializeField]
    TextMeshProUGUI bowDataText;

    [Space]
    [SerializeField]
    Outline[] swordOutlines;
    [SerializeField]
    TextMeshProUGUI swordDataText;

    [Space]
    [Header("Debug")]
    [SerializeField]
    [TestButton("Update Display", "ShadeChange")]
    PlayerStats currentStats;
    [SerializeField]
    bool isHoldingBow;

    private PlayerController localPlayerController;

    private void OnEnable()
    {
        MatchManager.onMatchStart += HandleMatchStart;
        if (localPlayerController != null)
        {
            localPlayerController.onShadeChange += HandleShadeChange;
            localPlayerController.GetComponent<PlayerWeaponHolding>().onWeaponChange += HandleWeaponChange;
        }
    }

    private void HandleMatchStart()
    {
        if (MatchManager.Instance.localPlayerController != null)
        {
            //Set variables
            localPlayerController = MatchManager.Instance.localPlayerController;
            currentStats = localPlayerController.CurrentStats;

            //Set events
            localPlayerController.onShadeChange += HandleShadeChange;
            localPlayerController.GetComponent<PlayerWeaponHolding>().onWeaponChange += HandleWeaponChange;
        }
        ShadeChange();
    }

    private void OnDisable()
    {
        MatchManager.onMatchStart -= HandleMatchStart;
        if (localPlayerController != null)
        {
            localPlayerController.onShadeChange -= HandleShadeChange;
            localPlayerController.GetComponent<PlayerWeaponHolding>().onWeaponChange -= HandleWeaponChange;
        }
    }

    private void HandleShadeChange(PlayerStats stats)
    {
        currentStats = stats;
        ShadeChange();
    }


    private void HandleWeaponChange(WeaponHeld weaponHeld)
    {
        Debug.Log("HandleWeaponChangeUI "+weaponHeld);

        isHoldingBow = weaponHeld == WeaponHeld.BOW;
        ShadeChange();
    }


    private void ShadeChange()
    {
        //Handle Bow
        if(currentStats.armedWithBow == false)
        {
            isHoldingBow = false;
        }

        bowPanel.SetActive(currentStats.armedWithBow);

        bowDataText.text = "Damage x" + currentStats.bowDamageMultiplier + "\n" +
            "Knockback x" + currentStats.bowKnockbackMultiplier;

        //Handle Sword
        swordDataText.text = "Damage: " + currentStats.damageDealt + "\n" +
            "Knockback x" + currentStats.swordKnockbackMultiplier;

        //Handle Outlines/Selection
        Outline[] selected;
        Outline[] unselected;

        if(isHoldingBow)
        {
            selected = bowOutlines;
            unselected = swordOutlines;
        }
        else
        {
            unselected = bowOutlines;
            selected = swordOutlines;
        }

        UpdateOutlines(selected, selectedOutlineThickness, selectedOutlineColor);
        UpdateOutlines(unselected, unselectedOutlineThickness, unselectedOutlineColor);
    }

    private void UpdateOutlines(Outline[] outlines, float thickness, Color color)
    {
        foreach(Outline e in outlines)
        {
            e.effectColor = color;
            e.effectDistance = new Vector2(thickness, -thickness);
        }
    }
}
