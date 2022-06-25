using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerController : NetworkBehaviour
{
    Team currentTeam = Team.INVALID;

    [SerializeField]
    Material blackDebug;
    [SerializeField]
    Material whiteDebug;

    [ClientRpc]
    public void AssignTeamClientRpc(Team _team)
    {
        currentTeam = _team;

        //For Debugging Purposes

        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();

        if(currentTeam == Team.BLACK)
        {
            meshRenderer.material = blackDebug;
        }
        else
        {
            meshRenderer.material = whiteDebug;
        }
        //End of "For Debugging Purposes"
    }
}
