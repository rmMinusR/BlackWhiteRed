using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMoveController : NetworkBehaviour
{
    [Header("Movement settings")]
    [SerializeField] [Range(0, 1)] private float groundSlipperiness = 0.1f;
    [SerializeField] [Range(0, 1)] private float airSlipperiness = 0.1f;
    [SerializeField] [Min(0)] private float _speed = 5f;
    [SerializeField] [Min(0)] private float jumpPower = 5f;
    public float Speed => _speed;
    public float CurrentSlipperiness => kinematicsLayer.frame.isGrounded ? groundSlipperiness : airSlipperiness;

    [SerializeField] [Min(0)] private float jumpCooldown = 0.5f;

    [Header("Bindings")]
    [SerializeField] private CharacterKinematics kinematicsLayer;
    [SerializeField] private Transform frameOfReference;
    [SerializeField] private PlayerInput playerInput;
    private InputAction controlMove;
    [SerializeField] private string controlMoveName = "Move";
    private InputAction controlJump;
    [SerializeField] private string controlJumpName = "Jump";

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsLocalPlayer)
        {
            controlMove = playerInput.currentActionMap.FindAction(controlMoveName);
            controlJump = playerInput.currentActionMap.FindAction(controlJumpName);

            kinematicsLayer.PreMove -= UpdateInput;
            kinematicsLayer.PreMove += UpdateInput;
        }

        kinematicsLayer.PreMove -= ApplyMovement;
        kinematicsLayer.PreMove += ApplyMovement;
    }

    private void UpdateInput(ref PlayerPhysicsFrame frame, bool live)
    {
        //Don't read input if simulating
        if (!live) return;

        frame.input = controlMove.ReadValue<Vector2>();
        frame.jump = controlJump.IsPressed();
    }

    public void ApplyMovement(ref PlayerPhysicsFrame frame, bool live)
    {
        //Handle horizontal movement
        Vector3 targetVelocity = Speed*( frame.Right   * frame.input.x
                                       + frame.Forward * frame.input.y );

        float slippageThisFrame = Mathf.Pow(CurrentSlipperiness, Time.fixedDeltaTime);
        Vector3 newVel = Vector3.Lerp(targetVelocity, frame.velocity, slippageThisFrame);
        frame.velocity = new Vector3(newVel.x, frame.velocity.y, newVel.z);

        //Handle jumping
        if (frame.jump)
        {
            frame.timeCanNextJump = (float)NetworkManager.ServerTime.FixedTime + jumpCooldown;
            frame.velocity += jumpPower * transform.up;
            frame.isGrounded = false;
        }
    }
}
