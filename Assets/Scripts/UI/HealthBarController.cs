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

    // Start is called before the first frame update
    void Start()
    {
        images = new List<Image>();
        SetUpChildren();
    }

    private void SetUpChildren()
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

        HandleNewCurrentHealth();
    }

    private void HandleNewCurrentHealth()
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
}
