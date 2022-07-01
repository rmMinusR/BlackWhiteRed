using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    // Start is called before the first frame update
    void Start()
    {
        ShadeChange();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void HandleShadeChange(PlayerStats stats)
    {
        currentStats = stats;
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

        bowDataText.text = "Damage x" + currentStats.damageMultiplier + "\n" +
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
