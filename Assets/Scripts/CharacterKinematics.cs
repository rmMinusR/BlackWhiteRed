using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public sealed class CharacterKinematics : NetworkBehaviour
{
    private CharacterController coll { get; set; }

    private void Awake()
    {
        coll = GetComponent<CharacterController>();
    }

    private void FixedUpdate()
    {
        ApplyPendingTeleportation();
        if (!transform.hasChanged) Step(Time.fixedTime, Time.fixedDeltaTime);
    }

    //Transient I/O
#if UNITY_EDITOR
    [InspectorReadOnly]
#else
    [NonSerialized]
#endif
    public Vector3 velocity;

#if UNITY_EDITOR
    [InspectorReadOnly]
#else
    [NonSerialized]
#endif
    public CollisionFlags _contactAreas;
    public CollisionFlags contactAreas {
        get => _contactAreas;
        private set => _contactAreas = value;
    }

    [Header("Gravity")]
    public bool enableGravity = true;
    public float gravityScale = 1;
    public Vector3 RawGravityExperienced => enableGravity ? Physics.gravity*gravityScale : Vector3.zero;
    [SerializeField] private bool suspendGravityOnGround = true;

    [Header("Ground detection")]
    [SerializeField] [Min(0)] private float groundProbeDistance = 0.1f;
    private float timeSinceLastGround;
    [SerializeField] [Min(0)] private float coyoteTime = 0.12f;
    public bool IsGrounded => timeSinceLastGround < coyoteTime;
    [SerializeField] private bool _isGrounded;
    private float Groundedness => suspendGravityOnGround ? 0 : Mathf.Clamp01(timeSinceLastGround/coyoteTime);
    public void MarkUngrounded() => timeSinceLastGround = coyoteTime;

    private void Step(float t, float dt)
    {
        //Detect ground state
        timeSinceLastGround += dt;
        if (false && IsLocalPlayer)
        {
            //Collider method is reliable only on owning client
            if (coll.isGrounded) timeSinceLastGround = 0;
        }
        else
        {
            //Everything but the Player and Ignore Raycast layers
            if (Physics.Raycast(transform.position, Vector3.down, coll.height/2+groundProbeDistance+coll.skinWidth, ~(1<<6 | 1<<2))) timeSinceLastGround = 0;
        }

        //Gravity
        velocity += RawGravityExperienced * (1-Groundedness) * dt;

        //Move step
        contactAreas = coll.Move(velocity * dt);

        //DEBUG ONLY
        _isGrounded = IsGrounded;
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        //Prevent building speed running into walls
        velocity = Vector3.ProjectOnPlane(velocity, hit.normal);
    }

    //External force and teleportation interface
    public void Teleport(Vector3 pos, Vector3? vel = null)
    {
        if (!IsServer) throw new AccessViolationException("Server frame is authority! Can only teleport on serverside.");

        teleportPending = true;
        teleportPos = pos;

        teleportVel = vel ?? Vector3.zero;
    }
#if UNITY_EDITOR
    [Header("Teleportation (manual control)")]
    [InspectorReadOnly(playing = AccessMode.ReadWrite)] [SerializeField] private bool teleportPending = false;
    [InspectorReadOnly(playing = AccessMode.ReadWrite)] [SerializeField] private Vector3 teleportPos;
    [InspectorReadOnly(playing = AccessMode.ReadWrite)] [SerializeField] private Vector3 teleportVel;
#else
    private bool teleportSetPos = false;
    private Vector3 teleportPos;
    private bool teleportSetVel = false;
    private Vector3 teleportVel;
#endif
    private void ApplyPendingTeleportation()
    {
        if (teleportPending) {
            DONOTCALL_TeleportPlayerSide_ClientRPC(teleportPos, teleportVel, ClientIDCache.Narrowcast(OwnerClientId));
            DONOTCALL_SetKinematics(teleportPos, teleportVel);
            teleportPending = false;
        }
    }

    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    private void DONOTCALL_TeleportPlayerSide_ClientRPC(Vector3 pos, Vector3 vel, ClientRpcParams p)
    {
        DONOTCALL_SetKinematics(pos, vel);
    }

    private void DONOTCALL_SetKinematics(Vector3 pos, Vector3 vel)
    {
        if (IsServer) {
            if (TryGetComponent(out PlayerDeadReckoner d)) d.SetAuthorityFrame(pos, vel);
            else
            {
                transform.position = pos;
                velocity = vel;
            }
        }
    }
}
