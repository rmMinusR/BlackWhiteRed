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

    [Header("Network")]
    [SerializeField] private ulong playerID;

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
        //Handle horizontal movement
        Vector3 targetVelocity = speed * (
                frameOfReference.right   * rawInput.x
              + frameOfReference.forward * rawInput.y
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
