using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMoveController : NetworkBehaviour
{
    [Header("Movement settings")]
    [SerializeField] [Range(0, 1)] private float groundSlipperiness = 0.1f;
    [SerializeField] [Range(0, 1)] private float airSlipperiness = 0.1f;
    [SerializeField] [Min(0)] private float speed = 5f;
    [SerializeField] [Min(0)] private float jumpPower = 5f;
    [SerializeField] [Min(0)] private float jumpCooldown = 0.5f;

    [Header("Bindings")]
    [SerializeField] private CharacterKinematics kinematicsLayer;
    [SerializeField] private Transform frameOfReference;
    [SerializeField] private PlayerInput playerInput;
    private InputAction controlMove;
    [SerializeField] private string controlMoveName = "Move";
    private InputAction controlJump;
    [SerializeField] private string controlJumpName = "Jump";

    private void Start()
    {
        kinematicsLayer.PreMove -= ApplyMovement;
        kinematicsLayer.PreMove += ApplyMovement;

        kinematicsLayer.PreMove -= UpdateInput;
        kinematicsLayer.PreMove += UpdateInput;

        controlMove = playerInput.actions.FindActionMap(playerInput.defaultActionMap).FindAction(controlMoveName);
        controlJump = playerInput.actions.FindActionMap(playerInput.defaultActionMap).FindAction(controlJumpName);
    }

    private Vector2 moveState;
    private void UpdateMoveState(InputAction.CallbackContext c) => moveState = c.ReadValue<Vector2>();

    public override void OnDestroy()
    {
        base.OnDestroy();

        kinematicsLayer.PreMove -= UpdateInput;

        controlMove.performed -= UpdateMoveState;
        controlMove.canceled  -= UpdateMoveState;
        controlMove.started   -= UpdateMoveState;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsLocalPlayer)
        {
            playerInput.gameObject.SetActive(true);
            playerInput.enabled = true;
            playerInput.ActivateInput();

            controlMove = playerInput.actions.FindActionMap(playerInput.defaultActionMap).FindAction(controlMoveName);
            controlJump = playerInput.actions.FindActionMap(playerInput.defaultActionMap).FindAction(controlJumpName);

            controlMove.performed -= UpdateMoveState;
            controlMove.canceled  -= UpdateMoveState;
            controlMove.started   -= UpdateMoveState;
            controlMove.performed += UpdateMoveState;
            controlMove.canceled  += UpdateMoveState;
            controlMove.started   += UpdateMoveState;
        }
    }

    private void UpdateInput(ref PlayerPhysicsFrame frame, CharacterKinematics.StepMode mode)
    {
        //Don't read input if simulating, or if we're a remote player
        if (mode == CharacterKinematics.StepMode.LiveForward)
        {
            frame.input = moveState; //controlMove.ReadValue<Vector2>();
            frame.jump = controlJump.IsPressed();
        }
    }

    public void ApplyMovement(ref PlayerPhysicsFrame frame, CharacterKinematics.StepMode mode)
    {
        //Handle horizontal movement
        Vector3 targetVelocity = speed*( frame.Right   * frame.input.x
                                       + frame.Forward * frame.input.y );

        float slippageThisFrame = Mathf.Pow(frame.isGrounded ? groundSlipperiness : airSlipperiness, Time.fixedDeltaTime);
        Vector3 newVel = Vector3.Lerp(targetVelocity, frame.velocity, slippageThisFrame);
        frame.velocity = new Vector3(newVel.x, frame.velocity.y, newVel.z);

        //Handle jumping
        if (frame.jump && frame.isGrounded)
        {
            frame.timeCanNextJump = (float)NetworkManager.ServerTime.FixedTime + jumpCooldown;
            frame.velocity += jumpPower * transform.up;
        }
    }
}
