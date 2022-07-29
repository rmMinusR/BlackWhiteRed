using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class PortalController : MonoBehaviour
{
    [SerializeField]
    Team team;

    private void Awake()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Portal Triggered By " + other.name);

        //Must be the server
        if(!NetworkManager.Singleton.IsServer)
        {
            Debug.Log("Portal not in server");

            return;
        }

        //Must be set to a team
        if(team == Team.INVALID)
        {
            Debug.Log("Portal not with team");
            return;
        }

        PlayerController pc;
        if(other.TryGetComponent<PlayerController>(out pc))
        {
            if(pc.CurrentTeam != team)
            {
                /* I'm aware that a public method may not be good 
                 * if cheaters became a problem but this was to make 
                 * sure Dan had access to the prefabs working ASAP.
                 * If you think I need to go back and fix this, 
                 * let me know!
                 * - Nick
                 */
                MatchManager.Instance.HandlePortalScore(pc);
            }
        }
    }

}
