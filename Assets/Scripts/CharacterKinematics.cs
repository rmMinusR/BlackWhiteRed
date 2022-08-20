﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public sealed class CharacterKinematics : NetworkBehaviour
{
    [Header("Physics representation")]
#if UNITY_EDITOR
    [SerializeField] [InspectorReadOnly]
#endif
    private CharacterController _coll;
    private CharacterController coll => _coll != null ? _coll : (_coll = GetComponent<CharacterController>());

#if UNITY_EDITOR
    private void __RebuildProjection() => proj = ProjectionShape.Build(gameObject);
    [TestButton("Rebuild projection", nameof(__RebuildProjection), isActiveAtRuntime = true, isActiveInEditor = false, order = 1)]
    [SubclassSelector(order = 2)] [SerializeReference] [InspectorReadOnly(editing = AccessMode.ReadOnly, playing = AccessMode.ReadWrite)]
#endif
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

    [SerializeField] [Range(0, 1)] private float visualSmoothing = 0.99f;

    private void FixedUpdate()
    {
        //Derive next kinematics frame
        frame = Step(frame, (float)NetworkManager.ServerTime.FixedTime - frame.time, IsLocalPlayer ? StepMode.LiveForward : StepMode.LiveSpeculation); //Only local player has live input. Anything serverside is speculation until proven otherwise.

        //Apply
        if (IsClient && PlayerPhysicsFrame.DoSmoothing(frame.type))
        {
            Vector3 forwardProjectedPos = frame.position + frame.velocity * Time.fixedDeltaTime / (1 - visualSmoothing);
            transform.position = Vector3.Lerp(forwardProjectedPos, transform.position, visualSmoothing);
        }
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

    public delegate void MoveDelegate(ref PlayerPhysicsFrame frame, float dt, StepMode mode);
    public event MoveDelegate PreMove  = default; //Intended for input fetching. Should be pure. Applied in Step().
    public event MoveDelegate MoveStep = default; //Intended for logic that changes kinematics data. Should be pure. Applied in Step().
    public event Action FinalizeMove = default; //Called after a live frame has been applied, in FixedUpdate()

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

    [Header("Collision repsonse")]
    [SerializeField] [Min(0)] private float raycastEpsilon = 0.01f;
    [SerializeField] private LayerMask ignoredLayers = 1<<6 | 1<<2 | 1<<3 | 1<<8; //The Player, Ignore Raycast, Shade, and Arrow layers. TODO fetch from Physics at runtime
    private LayerMask InteractableLayers => ~ignoredLayers;

    [Pure] //Only if mode != StepMode.Live
    public PlayerPhysicsFrame Step(PlayerPhysicsFrame frame, float dt, StepMode mode)
    {
        if (mode != StepMode.SimulateRecalc) frame.type = PlayerPhysicsFrame.Type.Default;
        frame.time += dt;
        ++frame.id;
        
        //Update ground state
        frame.timeSinceLastGround += dt;
        
        //Ground check
        if (Physics.CheckSphere(frame.position + Vector3.down*(coll.height/2-coll.radius+groundProbeOffset), coll.radius+groundProbeRadius, InteractableLayers)) frame.timeSinceLastGround = 0;
        frame.isGrounded = frame.timeSinceLastGround < coyoteTime;

        //Gravity
        float currentGravityStrength = (suspendGravityOnGround && frame.isGrounded) ? Mathf.Clamp01(frame.timeSinceLastGround/coyoteTime) : 1;
        frame.velocity += RawGravityExperienced * dt * currentGravityStrength;
        
        //Custom logic hooks
        if (PreMove  != null) PreMove (ref frame, dt, mode);
        if (MoveStep != null) MoveStep(ref frame, dt, mode);

        //Apply velocity
        Vector3 move = frame.velocity*dt;
        if (PlayerPhysicsFrame.DoCollisionTest(frame.type) && proj.Shapecast(out RaycastHit hit, frame.position, move.normalized, move.magnitude, InteractableLayers))
        {
            //Collision response
            move = move.normalized * hit.distance;
            frame.velocity = Vector3.ProjectOnPlane(frame.velocity, hit.normal);

            //Prevent clipping - Low precision
            frame.position += hit.normal * raycastEpsilon;
        }
        frame.position += move;

        return frame;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 pos = Application.isPlaying ? frame.position : transform.position;

        //Draw projection
        Gizmos.color = Color.green;
        if (proj != null) proj.DrawAsGizmos(pos);

        //Draw ground sensor
        Gizmos.color = frame.isGrounded ? Color.yellow : Color.red;
        Gizmos.DrawWireSphere(pos + Vector3.down*(coll.height/2-coll.radius+groundProbeOffset), coll.radius+groundProbeRadius);
    }
}
