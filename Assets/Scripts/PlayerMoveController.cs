using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMoveController : MonoBehaviour
{
    [Header("Movement settings")]
    [SerializeField] [Range(0, 1)] private float groundSlipperiness = 0.1f;
    [SerializeField] [Range(0, 1)] private float airSlipperiness = 0.1f;
    [SerializeField] [Min(0)] private float _speed = 5f;
    [SerializeField] [Min(0)] private float jumpPower = 5f;
    public float Speed => _speed;
    public float CurrentSlipperiness => kinematicsLayer.coll.isGrounded ? groundSlipperiness : airSlipperiness;

    [Header("Bindings")]
    [SerializeField] private CharacterKinematics kinematicsLayer;
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

    public Vector2 rawInput { get; private set; }
    public bool jumpPressed { get; private set; }

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
        Vector3 targetVelocity = Speed * (
                localRight * rawInput.x
              + localForward * rawInput.y
            );

        float slippageThisFrame = Mathf.Pow(CurrentSlipperiness, Time.fixedDeltaTime);
        Vector3 newVel = Vector3.Lerp(targetVelocity, kinematicsLayer.velocity, slippageThisFrame);
        kinematicsLayer.velocity = new Vector3(newVel.x, kinematicsLayer.velocity.y, newVel.z);

        //Handle jumping
        if (jumpPressed && kinematicsLayer.coll.isGrounded)
        {
            kinematicsLayer.velocity += jumpPower * transform.up;
        }
    }
}
