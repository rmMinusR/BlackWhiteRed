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
    [SerializeField] private PlayerInput playerInput;
    private InputAction controlMove;
    [SerializeField] private string controlMoveName = "Move";
    private InputAction controlJump;
    [SerializeField] private string controlJumpName = "Jump";

    private void Start()
    {
        kinematicsLayer.PreMove -= UpdateInput;
        kinematicsLayer.PreMove += UpdateInput;

        kinematicsLayer.MoveStep -= ApplyMovement;
        kinematicsLayer.MoveStep += ApplyMovement;

        controlMove = playerInput.actions.FindActionMap(playerInput.defaultActionMap).FindAction(controlMoveName);
        controlJump = playerInput.actions.FindActionMap(playerInput.defaultActionMap).FindAction(controlJumpName);
    }

    private Vector2 moveState;
    private void UpdateMoveState(InputAction.CallbackContext c) => moveState = c.ReadValue<Vector2>();

    public override void OnDestroy()
    {
        base.OnDestroy();

        kinematicsLayer.PreMove -= UpdateInput;

        kinematicsLayer.MoveStep -= ApplyMovement;

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

    private void UpdateInput(ref PlayerPhysicsFrame frame, float dt, CharacterKinematics.StepMode mode)
    {
        //Don't read input if simulating, or if we're a remote player
        //Also suppress if we're teleporting
        if (mode == CharacterKinematics.StepMode.LiveForward && frame.type == PlayerPhysicsFrame.Type.NormalMove && PlayerLookController.cursorLocked)
        {
            frame.input.move = moveState; //controlMove.ReadValue<Vector2>();
            frame.input.jump = controlJump.IsPressed();
        }
    }

    public void ApplyMovement(ref PlayerPhysicsFrame frame, float dt, CharacterKinematics.StepMode mode)
    {
        //Handle horizontal movement
        Vector3 targetVelocity = speed*( frame.Right   * frame.input.move.x
                                       + frame.Forward * frame.input.move.y );

        float slippageThisFrame = Mathf.Pow(frame.isGrounded ? groundSlipperiness : airSlipperiness, dt);
        frame.velocity.x = Mathf.Lerp(targetVelocity.x, frame.velocity.x, slippageThisFrame);
        frame.velocity.z = Mathf.Lerp(targetVelocity.z, frame.velocity.z, slippageThisFrame);

        //Handle jumping
        if (frame.input.jump && frame.isGrounded && frame.timeCanNextJump < frame.time)
        {
            frame.timeCanNextJump = (float)NetworkManager.ServerTime.FixedTime + jumpCooldown;
            frame.velocity.y += jumpPower;
        }
    }
}
