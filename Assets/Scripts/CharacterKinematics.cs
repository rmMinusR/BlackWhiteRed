using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public sealed class CharacterKinematics : MonoBehaviour
{
    public CharacterController coll { get; private set; }

    private void Start()
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

    //Tweakable in inspector, but mutable at runtime
    public bool enableGravity = true;
    public float gravityScale = 1;
    public Vector3 GravityExperienced => enableGravity ? Physics.gravity*gravityScale : Vector3.zero;

    //Fixed settings
    [SerializeField] private bool suspendGravityOnGround = true;

    private void Step(float t, float dt)
    {
        //May cause funky behaviour going down slopes. TODO test
        if (!suspendGravityOnGround || !coll.isGrounded) velocity += GravityExperienced * dt;

        contactAreas = coll.Move(velocity * dt);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        //Prevent building speed running into walls
        velocity = Vector3.ProjectOnPlane(velocity, hit.normal);
    }
}
