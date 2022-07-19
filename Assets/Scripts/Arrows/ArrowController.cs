using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ArrowController : NetworkBehaviour
{
    [SerializeField]
    Transform appearance;
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
    ulong shooterId;

    [SerializeField]
    Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    [ClientRpc]
    public void InitClientRpc(Team _team, ulong _shooterId, int _shadeValue, Vector3 startingPosition, Vector3 startDirection, float amountCharged, float timeShot)
    {
        Debug.Log("Arrow Inited");

        Vector3 startingVelocity = startDirection * Mathf.Lerp(minimumStartingVelocity, maximumStartingVelocity, amountCharged);
        float timeSinceShot = NetworkManager.Singleton.LocalTime.TimeAsFloat - timeShot;

        gameObject.SetActive(true);
        team = _team;
        shadeValue = _shadeValue;
        shooterId = _shooterId;
        transform.position = startingPosition + startingVelocity * timeSinceShot + .5f * Physics.gravity * timeSinceShot * timeSinceShot;
        rb.velocity = startingVelocity + Physics.gravity * timeSinceShot;

        if (IsServer || IsHost)
        {
            timer = timeBeforeDespawn;
        }
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

    private void FixedUpdate()
    {
        appearance.forward = rb.velocity;
    }
}
