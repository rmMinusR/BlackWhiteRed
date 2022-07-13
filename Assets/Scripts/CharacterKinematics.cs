﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public sealed class CharacterKinematics : NetworkBehaviour
{
    private CharacterController coll;
    private ProjectionShape proj;
    [SerializeField] private PlayerMoveController move;
    [SerializeField] private PlayerLookController look;


    private void Awake()
    {
        coll = GetComponent<CharacterController>();
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
        bool didTeleport = ApplyPendingTeleportation();

        //Derive and apply next kinematics frame
        frame = Step(frame, Time.fixedDeltaTime, IsLocalPlayer); //Only local player has live input. Anything serverside is speculation until proven otherwise.
        coll.Move(frame.position - transform.position);

        if (FinalizeMove != null) FinalizeMove();
    }

    public delegate void MoveDelegate(ref PlayerPhysicsFrame frame, bool live);
    public event MoveDelegate  PreMove = default; //Must be pure. Applied in Step().
    public event MoveDelegate PostMove = default; //Must be pure. Applied in Step().
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
    [SerializeField] [Min(0)] private float groundProbeDistance = 0.1f;
    [SerializeField] [Min(0)] private float coyoteTime = 0.12f;

    [Pure] //Only if live=false
    public PlayerPhysicsFrame Step(PlayerPhysicsFrame frame, float dt, bool live)
    {
        if (PreMove != null) PreMove(ref frame, live);

        //Update ground state
        frame.timeSinceLastGround += dt;
        
        //Ground check = everything but the Player and Ignore Raycast layers
        if (Physics.CheckSphere(frame.position + Vector3.down*(coll.height-coll.radius)/2, coll.radius+2*coll.skinWidth, ~(1<<6 | 1<<2))) frame.timeSinceLastGround = 0;

        frame.isGrounded = frame.timeSinceLastGround < coyoteTime;

        //Gravity
        frame.velocity += RawGravityExperienced * (1-Mathf.Clamp01(frame.timeSinceLastGround/coyoteTime)) * dt;
        
        //Move step
        Vector3 move = frame.velocity*dt;
        if (proj.Shapecast(out RaycastHit hit, frame.position, move, Quaternion.identity, move.magnitude))
        {
            //Collision response
            move = move.normalized * hit.distance;
            frame.velocity = Vector3.ProjectOnPlane(frame.velocity, hit.normal);
        }
        frame.position += move;

        if (PostMove != null) PostMove(ref frame, live);

        return frame;
    }

    //Teleportation
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
    private bool teleportPending = false;
    private Vector3 teleportPos;
    private Vector3 teleportVel;
#endif
    private bool ApplyPendingTeleportation()
    {
        if (!IsServer) return false;

        if (teleportPending) {
            teleportPending = false;
            //TODO FIXME reimplement
            return true;
        }
        return false;
    }
}
