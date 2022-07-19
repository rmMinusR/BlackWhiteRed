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
    LayerMask collisionDetectionMask;

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
        rb.constraints = RigidbodyConstraints.None;

        team = _team;
        shadeValue = _shadeValue;
        shooterId = _shooterId;
        transform.position = startingPosition + startingVelocity * timeSinceShot + .5f * Physics.gravity * timeSinceShot * timeSinceShot;
        rb.velocity = startingVelocity + Physics.gravity * timeSinceShot;

        if (IsServer || IsHost)
        {
            //Check with a sphere cast for if it has already hit a wall or entity
            RaycastHit raycastHit;
            Ray ray = new Ray(startingPosition, transform.position);
            if(Physics.SphereCast(ray, .25f, out raycastHit, Vector3.Distance(startingPosition, transform.position), collisionDetectionMask))
            {
                ProcessCollision(raycastHit.collider);
            }
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
        if (rb.constraints == RigidbodyConstraints.None)
        {
            appearance.forward = rb.velocity;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer && !IsHost)
        {
            return;
        }

        ProcessCollision(other);
    }

    private void ProcessCollision(Collider hit)
    {

        //Handle ground, bomb, or enemy teammate
        switch (hit.gameObject.layer)
        {
            //Ground
            case 0:
                StickIntoPlaceClientRpc(hit.ClosestPoint(transform.position));
                timer = timeBeforeDespawn;
                break;
            //Players
            case 6:
                PlayerController playerController = hit.GetComponent<PlayerController>();
                if (playerController.Team != team)
                {
                    playerController.GetComponent<PlayerHealth>().TakeDamage(
                        rb.velocity.magnitude * kit.playerStats[shadeValue].bowDamageMultiplier, 
                        DamageSource.ARROW, 
                        NetworkManager.Singleton.SpawnManager.SpawnedObjects[shooterId].GetComponent<PlayerController>());
                    ArrowPool.Instance.UnloadArrow(gameObject);
                }
                break;
            //Bomb
            case 9:
                ArrowPool.Instance.UnloadArrow(gameObject);
                break;
            default:
                break;
        }
    }

    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    private void StickIntoPlaceClientRpc(Vector3 pos)
    {
        rb.constraints = RigidbodyConstraints.FreezeAll;
        transform.position = pos;
    }
}
