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
    float damageMultiplier = 0.1f;

    [SerializeField]
    [Min(10)]
    float timeBeforeDespawn;
    [SerializeField]
    [Min(1)]
    float timeBeforeDespawnOnceLanded;

    float timer;

    //PlayerController shooter;
    Team team;
    int shadeValue;
    ulong shooterId;

    bool landed = false;

    [SerializeField]
    Rigidbody rb;

    [SerializeField]
    SphereCollider sphereCollider;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    [ClientRpc(Delivery = RpcDelivery.Reliable)]
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

        timer = timeBeforeDespawn;
        appearance.forward = rb.velocity;
        landed = false;

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

        sphereCollider.enabled = true;
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
        if ((!IsServer && !IsHost) || landed)
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
                Debug.Log("Arrow hit ground " + hit.name);
                StickIntoPlaceClientRpc(hit.ClosestPoint(transform.position));
                timer = timeBeforeDespawnOnceLanded;
                landed = true;
                sphereCollider.enabled = false;
                break;
            //Players
            case 6:
                Debug.Log("Arrow hit player " + hit.name);
                PlayerController playerController = hit.GetComponent<PlayerController>();
                if (playerController.CurrentTeam != team)
                {
                    //Knockback
                    playerController.GetComponent<PlayerKnockbackController>().KnockbackPlayer(rb.velocity, kit.playerStats[shadeValue].bowKnockbackMultiplier);

                    //Damage
                    playerController.GetComponent<PlayerHealth>().TakeDamage(
                        rb.velocity.magnitude * damageMultiplier * kit.playerStats[shadeValue].bowDamageMultiplier,
                        DamageSource.ARROW,
                        NetworkManager.Singleton.SpawnManager.SpawnedObjects[shooterId].GetComponent<PlayerController>());

                    //Unload
                    sphereCollider.enabled = false;
                    ArrowPool.Instance.UnloadArrow(gameObject);
                }
                break;
            //Bomb
            case 9:
                sphereCollider.enabled = false;
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
        sphereCollider.enabled = false;
        transform.position = pos;
    }
}
