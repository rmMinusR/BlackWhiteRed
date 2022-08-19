using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct RenderModelState
{
    public Color color;
    public Color color2;
}

public class RenderModelDisplayController : MonoBehaviour
{
    [Header("Material Changes")]
    [SerializeField]
    RenderModelState darkTeam;
    [SerializeField]
    RenderModelState lightTeam;
    [SerializeField]
    Material targetMaterial;
    [SerializeField]
    [Min(1)]
    float timeInMaterial;
    [SerializeField]
    [Min(1)]
    float timeToFade;
    [Space]
    [Header("Rotation")]
    [SerializeField]
    float rotationSpeed;
    [Space]
    [Header("Animations")]
    [SerializeField]
    float timeToHoldWeapon;

    bool isDarkMaterial;
    bool isFading;
    float materialTimer;
    float weaponHoldingTimer;
    Animator anim;

    private void Start()
    {
        isDarkMaterial = true;
        isFading = false;

        targetMaterial.SetColor("_Color", darkTeam.color);
        targetMaterial.SetColor("_Color2", darkTeam.color2);

        materialTimer = timeInMaterial;

        anim = GetComponent<Animator>();
        weaponHoldingTimer = timeToHoldWeapon;
    }

    private void Update()
    {
        UpdateMaterials();
        UpdateAnimations();

        //Update Rotation
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
    }

    private void UpdateMaterials()
    {
        materialTimer -= Time.deltaTime;

        if(isFading)
        {
            RenderModelState from = isDarkMaterial ? lightTeam : darkTeam;
            RenderModelState to = !isDarkMaterial ? lightTeam : darkTeam;

            targetMaterial.SetColor("_Color", Color.Lerp(from.color,to.color,1-materialTimer/timeToFade));
            targetMaterial.SetColor("_Color2", Color.Lerp(from.color2,to.color2,1-materialTimer/timeToFade));
        }

        if(materialTimer < 0)
        {
            if(isFading)
            {
                isFading = false;
                materialTimer = timeInMaterial;
                RenderModelState to = !isDarkMaterial ? lightTeam : darkTeam;
                targetMaterial.SetColor("_Color", to.color);
                targetMaterial.SetColor("_Color2", to.color2);
            }
            else
            {
                isFading = true;
                materialTimer = timeToFade;
                isDarkMaterial = !isDarkMaterial;
            }
        }
    }

    private void UpdateAnimations()
    {
        weaponHoldingTimer -= Time.deltaTime;

        if(weaponHoldingTimer < 0)
        {
            weaponHoldingTimer += timeToHoldWeapon;
            anim.SetBool("Weapon", !anim.GetBool("Weapon"));
        }
    }
}
