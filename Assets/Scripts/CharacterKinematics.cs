﻿using System;
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
        //Derive next kinematics frame
        frame = Step(frame, (float)NetworkManager.ServerTime.FixedTime - frame.time, IsLocalPlayer ? StepMode.LiveForward : StepMode.LiveSpeculation); //Only local player has live input. Anything serverside is speculation until proven otherwise.

        //Apply
        if (PlayerPhysicsFrame.DoCollisionTest(frame.mode)) coll.Move(frame.position - transform.position);
        else transform.position = frame.position;

        if (FinalizeMove != null) FinalizeMove();
    }

    public enum StepMode
    {
        /// <summary>
        /// Owner only, stepping forward and grabbing live inputs
        /// </summary>
        LiveForward,

        /// <summary>
        /// Non-owning clients and server, stepping forward using best speculative guess
        /// </summary>
        LiveSpeculation,

        /// <summary>
        /// Server only, ensure frame is valid
        /// </summary>
        SimulateVerify,

        /// <summary>
        /// Client and server, when triggering a rollback
        /// </summary>
        SimulateRecalc
    }

    public delegate void MoveDelegate(ref PlayerPhysicsFrame frame, StepMode mode);
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

    public const int INTERACTABLE_LAYERS = ~(1<<6 | 1<<2 | 1<<3); //Everything but the Player, Ignore Raycast, and Shade layers

    [Pure] //Only if mode != StepMode.Live
    public PlayerPhysicsFrame Step(PlayerPhysicsFrame frame, float dt, StepMode mode)
    {
        if (mode == StepMode.LiveForward || mode == StepMode.LiveSpeculation) frame.mode = PlayerPhysicsFrame.Mode.Default;
        frame.time += dt;
        ++frame.id;
        
        //Update ground state
        frame.timeSinceLastGround += dt;
        
        //Ground check
        if (Physics.CheckSphere(frame.position + Vector3.down*(coll.height/2-coll.radius+groundProbeOffset), coll.radius+groundProbeRadius, INTERACTABLE_LAYERS)) frame.timeSinceLastGround = 0;
        frame.isGrounded = frame.timeSinceLastGround < coyoteTime;

        //Gravity
        if (!frame.isGrounded || !suspendGravityOnGround) frame.velocity += RawGravityExperienced * (1-Mathf.Clamp01(frame.timeSinceLastGround/coyoteTime)) * dt;
        
        //Custom logic hook
        if (PreMove != null) PreMove(ref frame, mode);
        
        //Move step
        Vector3 move = frame.velocity*dt;
        if (PlayerPhysicsFrame.DoCollisionTest(frame.mode) && proj.Shapecast(out RaycastHit hit, frame.position, move.normalized, Quaternion.identity, move.magnitude))
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
}
