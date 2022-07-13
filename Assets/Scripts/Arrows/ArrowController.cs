using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ArrowController : NetworkBehaviour
{
    [SerializeField]
    PlayerKit kit;

    [SerializeField]
    float minimumStartingVelocity;
    [SerializeField]
    float maximumStartingVelocity;

    [SerializeField]
    [Min(1)]
    float timeBeforeDespawn;

    float timer;

    //PlayerController shooter;
    Team team;
    int shadeValue;

    [SerializeField]
    Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Init(Team _team, int _shadeValue, Vector3 startingPosition, Vector3 startDirection, float amountCharged)
    {
        Debug.Log("Arrow Inited");

        gameObject.SetActive(true);
        team = _team;
        shadeValue = _shadeValue;
        transform.position = startingPosition;

        rb.velocity = startDirection * Mathf.Lerp(minimumStartingVelocity, maximumStartingVelocity, amountCharged);

        timer = timeBeforeDespawn;
    }

    private void Update()
    {
        if(timer > 0)
        {
            timer -= Time.deltaTime;
            if(timer <= 0)
            {
                ArrowPool.Instance.UnloadArrow(gameObject);
            }
        }
    }
}
