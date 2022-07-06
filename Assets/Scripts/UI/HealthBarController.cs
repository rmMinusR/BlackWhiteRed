using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBarController : MonoBehaviour
{
    [SerializeField]
    [Min(1)]
    int maxHealth;
    [SerializeField]
    [Min(0)]
    int currentHealth;

    [Space]
    [SerializeField]
    [Tooltip("Index 0 is for empty hearts, and indexes afterward are portions filled")]
    Sprite[] healthIcons;
    [SerializeField]
    GameObject imagePrefab;
    List<Image> images;

    PlayerHealth localPlayerHealth;

    void Start()
    {
        images = new List<Image>();
    }

    private void OnEnable()
    {
        MatchManager.onMatchStart += HandleMatchStart;
        if (localPlayerHealth != null)
        {
            localPlayerHealth.onHealthChange += HandleHealthChange;
        }
    }

    private void OnDisable()
    {
        MatchManager.onMatchStart -= HandleMatchStart;
        if (localPlayerHealth != null)
        {
            localPlayerHealth.onHealthChange -= HandleHealthChange;
        }
    }

    private void SetUpHealthDisplay()
    {
        if(healthIcons.Length < 2)
        {
            return;
        }

        int pointsPerImage = healthIcons.Length - 1;
        int imagesNeeded = Mathf.CeilToInt(1.0f * maxHealth / pointsPerImage);

        for(int i = 0; i < imagesNeeded; i++)
        {
            images.Add(Instantiate(imagePrefab,transform).GetComponent<Image>());
        }

        UpdateHealthDisplay();
    }

    private void UpdateHealthDisplay()
    {
        int pointsPerImage = healthIcons.Length - 1;
        int tempHealth = currentHealth;
        int tempClamp;
        foreach(Image e in images)
        {
            tempClamp = Mathf.Clamp(tempHealth, 0, pointsPerImage);
            e.sprite = healthIcons[tempClamp];
            tempHealth -= pointsPerImage;
        }
    }

    private void HandleMatchStart()
    {
        if (MatchManager.Instance.localPlayerController.TryGetComponent<PlayerHealth>(out localPlayerHealth))
        {
            //Set variables
            maxHealth = localPlayerHealth.GetMaxHealth();
            currentHealth = localPlayerHealth.GetCurrentHealth();

            //Set events
            localPlayerHealth.onHealthChange += HandleHealthChange;
        }
        SetUpHealthDisplay();
    }

    private void HandleHealthChange(int _value)
    {
        currentHealth = _value;
        UpdateHealthDisplay();
    }
}
