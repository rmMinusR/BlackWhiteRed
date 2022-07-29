using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;

public class PlayerController : NetworkBehaviour
{

    [SerializeField]
    [InspectorReadOnly]
    Team currentTeam = Team.INVALID;
    [SerializeField]
    [InspectorReadOnly]
    int shadeValue;
    [SerializeField]
    PlayerKit kit;

    [Space]
    [Header("Shade Detection")]
    [SerializeField]
    LayerMask shadeLayerMask;
    [SerializeField]
    float radiusCheck = 0.1f;

    [Space]
    [Header("Debugging")]
    [SerializeField]
    Material blackDebug;
    [SerializeField]
    Material whiteDebug;

    [SerializeField]
    Vector3 spawnPos;
    [SerializeField]
    Vector2 spawnLook;
    [SerializeField]
    TeleportController teleportController;

    public delegate void PlayerStatsEvent(PlayerStats _value);
    public event PlayerStatsEvent onShadeChange;

    public int ShadeValue => shadeValue;
    private PlayerStats currentStats => kit.playerStats[shadeValue];
    public Team CurrentTeam => currentTeam;
    public PlayerStats CurrentStats => currentStats;
    public int TeamValue => (int)currentTeam;

    [ClientRpc]
    public void AssignTeamClientRpc(Team _team, Vector3 _spawnPos, Vector2 _spawnLook)
    {
        currentTeam = _team;
        spawnPos = _spawnPos;
        spawnLook = _spawnLook;

        //For Debugging Purposes

        foreach(MeshRenderer meshRenderer in GetComponentsInChildren<MeshRenderer>())
        {
            if (currentTeam == Team.BLACK)
            {
                meshRenderer.material = blackDebug;
                shadeValue = 0;
            }
            else
            {
                meshRenderer.material = whiteDebug;
                shadeValue = 6;
            }
        }
        //End of "For Debugging Purposes"
    }

    private void OnEnable()
    {
        MatchManager.onMatchStart += HandleMatchStart;
        MatchManager.onTeamScore += HandleTeamScore;
        MatchManager.onTeamWin += HandleTeamScore;
    }

    private void OnDisable()
    {
        MatchManager.onMatchStart -= HandleMatchStart;
        MatchManager.onTeamScore -= HandleTeamScore;
        MatchManager.onTeamWin -= HandleTeamScore;
    }

    private void FixedUpdate()
    {
        CheckForNewShade();
    }

    private void CheckForNewShade()
    {
        //Ray r = new Ray(transform.position, Vector3.down);
        //RaycastHit hit;

        Vector3 pos = transform.position;

        Collider[] shadeBoxes = Physics.OverlapSphere(pos, radiusCheck, shadeLayerMask);

        if (shadeBoxes.Length > 0)
        {
            Collider currentShade = null;

            foreach(Collider e in shadeBoxes)
            {
                if(currentShade == null || (currentShade.ClosestPoint(pos) - pos).sqrMagnitude > (e.ClosestPoint(pos) - pos).sqrMagnitude)
                {
                    currentShade = e;
                }
            }

            int oldShadeValue = shadeValue;
            int temp = currentShade.GetComponent<ShadeData>().ShadeValue; 
            if(currentTeam == Team.WHITE)
            {
                temp *= -1;
                temp += 6;
            }
            shadeValue = temp;

            if(oldShadeValue != shadeValue)
            {
                OnShadeValueChange();
            }
        }

    }

    public void ResetToSpawnPoint()
    {
        Debug.Log(name +": Resetting Spawn Point " + (NetworkManager.Singleton.IsServer ? " (Client)" : " (Server)"));

        teleportController.Teleport(spawnPos, Vector3.zero, spawnLook);
    }

    private void OnShadeValueChange()
    {
        onShadeChange?.Invoke(currentStats);
    }

    private void HandleMatchStart()
    {
        ResetToSpawnPoint();
    }

    private void HandleTeamScore(Team team)
    {
        ResetToSpawnPoint();
    }
}
