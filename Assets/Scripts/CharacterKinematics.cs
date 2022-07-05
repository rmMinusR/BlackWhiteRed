using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public sealed class CharacterKinematics : MonoBehaviour
{
    private CharacterController coll { get; set; }

    private void Awake()
    {
        coll = GetComponent<CharacterController>();
    }

    private void FixedUpdate()
    {
        Step(Time.fixedTime, Time.fixedDeltaTime);
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

    private float timeSinceLastGround;
    private const float coyoteTime = 0.12f;
    public bool IsGrounded => timeSinceLastGround < coyoteTime;
    private float GravityCoyoteRatio => suspendGravityOnGround ? 1 : Mathf.Clamp01(1-timeSinceLastGround/coyoteTime);
    public void MarkUngrounded() => timeSinceLastGround = coyoteTime;

    //Tweakable in inspector, but mutable at runtime
    public bool enableGravity = true;
    public float gravityScale = 1;
    public Vector3 RawGravityExperienced => enableGravity ? Physics.gravity*gravityScale : Vector3.zero;

    //Fixed settings
    [SerializeField] private bool suspendGravityOnGround = true;

    private void Step(float t, float dt)
    {
        if (coll.isGrounded) timeSinceLastGround = 0;
        else timeSinceLastGround += dt;

        //May cause funky behaviour going down slopes. TODO test
        velocity += RawGravityExperienced * GravityCoyoteRatio * dt;

        //Move step
        contactAreas = coll.Move(velocity * dt);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        //Prevent building speed running into walls
        velocity = Vector3.ProjectOnPlane(velocity, hit.normal);
    }
}
