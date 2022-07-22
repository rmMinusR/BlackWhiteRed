using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPointMarker : MonoBehaviour
{
    [SerializeField]
    Team team;
    [SerializeField]
    public Vector2 look;

    // Start is called before the first frame update
    void Start()
    {
        MatchManager.Instance.SetSpawnPoint(team, this);
    }

    private void OnDrawGizmos()
    {
        Color display;

        if(team == Team.BLACK)
        {
            display = new Color(0.2f, 0.0f, 0.0f);
        }
        else
        {
            display = new Color(1.0f, 0.9f, 0.9f);
        }

        Gizmos.color = display;
        Gizmos.DrawWireCube(transform.position, new Vector3(1, 2, 1));
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 1.5f);
    }
}
