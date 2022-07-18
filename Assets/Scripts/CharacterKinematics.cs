using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public sealed class CharacterKinematics : NetworkBehaviour
{
    private CharacterController __coll;
    private CharacterController coll => __coll != null ? __coll : (__coll = GetComponent<CharacterController>());
    private ProjectionShape proj;
    [SerializeField] private PlayerMoveController move;
    [SerializeField] private PlayerLookController look;


    private void Awake()
    {
        proj = ProjectionShape.Build(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        frame.position = transform.position;
        frame.time = (float)NetworkManager.ServerTime.FixedTime;
    }

    private void FixedUpdate()
    {
        if (IsServer && teleportPending) ApplyPendingTeleportation();
        else
        {
            //Derive and apply next kinematics frame
            frame = Step(frame, (float)NetworkManager.ServerTime.FixedTime - frame.time, IsLocalPlayer); //Only local player has live input. Anything serverside is speculation until proven otherwise.
            coll.Move(frame.position - transform.position);
        } 

        if (FinalizeMove != null) FinalizeMove();
    }

    public delegate void MoveDelegate(ref PlayerPhysicsFrame frame, bool live);
    public event MoveDelegate PreMove = default; //Must be pure if live=true. Applied in Step().
    public event Action FinalizeMove = default; //Only applied in FixedUpdate()

    //Transient I/O
#if UNITY_EDITOR
    [InspectorReadOnly]
#else
    [NonSerialized]
#endif
    public PlayerPhysicsFrame frame;

    [Header("Gravity")]
    public bool enableGravity = true;
    public float gravityScale = 1;
    public Vector3 RawGravityExperienced => enableGravity ? Physics.gravity*gravityScale : Vector3.zero;
    [SerializeField] private bool suspendGravityOnGround = true;

    [Header("Ground detection")]
    [SerializeField] [Min(0)] private float groundProbeRadius = 0.05f;
    [SerializeField] [Min(0)] private float groundProbeOffset = 0.05f;
    [SerializeField] [Min(0)] private float coyoteTime = 0.12f;

    [Pure] //Only if live=false
    public PlayerPhysicsFrame Step(PlayerPhysicsFrame frame, float dt, bool live)
    {
        frame.mode = PlayerPhysicsFrame.Mode.NormalMove;
        frame.time += dt;

        //Update ground state
        frame.timeSinceLastGround += dt;
        
        //Ground check = everything but the Player and Ignore Raycast layers
        if (Physics.CheckSphere(frame.position + Vector3.down*(coll.height/2-coll.radius+groundProbeOffset), coll.radius+groundProbeRadius, ~(1<<6 | 1<<2))) frame.timeSinceLastGround = 0;
        
        frame.isGrounded = frame.timeSinceLastGround < coyoteTime;

        //Custom logic hook
        if (PreMove != null) PreMove(ref frame, live);

        //Gravity
        if(!frame.isGrounded || !suspendGravityOnGround) frame.velocity += RawGravityExperienced * (1-Mathf.Clamp01(frame.timeSinceLastGround/coyoteTime)) * dt;
        
        //Move step
        Vector3 move = frame.velocity*dt;
        if (proj.Shapecast(out RaycastHit hit, frame.position, move.normalized, Quaternion.identity, move.magnitude))
        {
            //Collision response
            move = move.normalized * hit.distance;
            frame.velocity = Vector3.ProjectOnPlane(frame.velocity, hit.normal);
        }
        frame.position += move;

        return frame;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + Vector3.down*(coll.height/2-coll.radius+groundProbeOffset), coll.radius+groundProbeRadius);
    }

    //Teleportation
    public void Teleport(Vector3 pos, Vector3? vel = null, Vector2? look = null) //TODO refactor to use PreMove instead!
    {
        if (!IsServer) throw new AccessViolationException("Server frame is authority! Can only teleport on serverside.");

        teleportPending = true;
        teleportPos = pos;

        teleportVel = vel ?? Vector3.zero;
        teleportLook = look ?? Vector2.zero;
    }
#if UNITY_EDITOR
    [Header("Teleportation (manual control)")]
    [InspectorReadOnly(playing = AccessMode.ReadWrite)] [SerializeField] private bool teleportPending = false;
    [InspectorReadOnly(playing = AccessMode.ReadWrite)] [SerializeField] private Vector3 teleportPos;
    [InspectorReadOnly(playing = AccessMode.ReadWrite)] [SerializeField] private Vector3 teleportVel;
    [InspectorReadOnly(playing = AccessMode.ReadWrite)] [SerializeField] private Vector3 teleportLook;
#else
    private bool teleportPending = false;
    private Vector3 teleportPos;
    private Vector3 teleportVel;
    private Vector3 teleportLook;
#endif
    private void ApplyPendingTeleportation()
    {
        if (!IsServer) throw new InvalidOperationException();

        if (teleportPending)
        {
            teleportPending = false;
            frame.mode = PlayerPhysicsFrame.Mode.Teleport;

            transform.position = teleportPos;
            frame.position = teleportPos;

            frame.look = teleportLook;
            look.angles = teleportLook;

            frame.velocity = teleportVel;
        }
    }
}
