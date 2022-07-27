using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Player Kit", menuName = "Player Kit")]
public class PlayerKit : ScriptableObject
{
    [SerializeField]
    public PlayerStats[] playerStats;
}
