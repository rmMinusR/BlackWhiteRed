using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMoveController : MonoBehaviour
{
    [Header("Movement settings")]
    [SerializeField] [Range(0, 1)] private float groundSlipperiness = 0.1f;
    [SerializeField] [Range(0, 1)] private float airSlipperiness = 0.1f;
    [SerializeField] [Min(0)] private float speed = 5f;
    [SerializeField] [Min(0)] private float jumpPower = 5f;

    [Header("Bindings")]
    [SerializeField] private KinematicCharacter kinematicsLayer;
    [SerializeField] private Transform frameOfReference;
    [SerializeField] private PlayerInput playerInput;
    private InputAction controlMove;
    [SerializeField] private string controlMoveName = "Move";
    private InputAction controlJump;
    [SerializeField] private string controlJumpName = "Jump";

    private void OnEnable()
    {
        controlMove = playerInput.currentActionMap.FindAction(controlMoveName);
        controlJump = playerInput.currentActionMap.FindAction(controlJumpName);
    }

    private Vector2 rawInput;
    private bool jumpPressed;

    private void Update()
    {
        rawInput = controlMove.ReadValue<Vector2>();
        jumpPressed = controlJump.IsPressed();
    }

    private void FixedUpdate()
    {
        Vector3 localRight = frameOfReference.right;
        localRight.y = 0;
        localRight.Normalize();
        Vector3 localForward = new Vector3(-localRight.z, 0, localRight.x);

        //Handle horizontal movement
        Vector3 targetVelocity = speed * (
                localRight * rawInput.x
              + localForward * rawInput.y
            );

        float slippageThisFrame = Mathf.Pow(kinematicsLayer.coll.isGrounded ? groundSlipperiness : airSlipperiness, Time.fixedDeltaTime);
        Vector3 newVel = Vector3.Lerp(targetVelocity, kinematicsLayer.velocity, slippageThisFrame);
        kinematicsLayer.velocity = new Vector3(newVel.x, kinematicsLayer.velocity.y, newVel.z);

        //Handle jumping
        if (jumpPressed && kinematicsLayer.coll.isGrounded)
        {
            kinematicsLayer.velocity += jumpPower * transform.up;
        }
    }
}
