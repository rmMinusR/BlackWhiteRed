using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PlayerLookController : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector2 sensitivity = Vector2.one;
    [SerializeField] [Range(-90,  0)] private float minVerticalAngle = -90;
    [SerializeField] [Range(  0, 90)] private float maxVerticalAngle = 90;

    [Header("Bindings")]
    [SerializeField] private CharacterKinematics kinematicsLayer;
    [SerializeField] private PlayerInput dataSource;
    private InputAction controlLook;
    [SerializeField] private string controlLookName = "Look";

    private static PlayerLookController cursorController = null;
    public static bool cursorLocked = true;

    private void OnEnable()
    {
        //Hook
        kinematicsLayer.PreMove -= UpdateLook;
        kinematicsLayer.PreMove += UpdateLook;

        //Auto capture
        controlLook = dataSource.actions.FindActionMap(dataSource.defaultActionMap).FindAction(controlLookName);
        controlLook.performed += BufferInput;
    }

    private Vector2 bufferedInput;
    private void BufferInput(InputAction.CallbackContext obj)
    {
        if (isActiveAndEnabled && cursorLocked && IsSpawned && IsLocalPlayer) bufferedInput += obj.ReadValue<Vector2>() / Camera.main.pixelRect.size.magnitude; //FIXME slow, cache Camera
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsLocalPlayer) cursorController = this;
    }

    private void UpdateLook(ref PlayerPhysicsFrame frame, CharacterKinematics.StepMode mode)
    {
        if (mode == CharacterKinematics.StepMode.LiveForward)
        {
            //Add given movement
            frame.look += bufferedInput * sensitivity;
            bufferedInput = Vector2.zero;

            if (cursorController == this)
            {
                //Lock cursor
                Cursor.lockState = cursorLocked ? CursorLockMode.Locked : CursorLockMode.None;
                Cursor.visible = !cursorLocked;
            }
        }

        //Limit
        frame.look.y = Mathf.Clamp(frame.look.y, minVerticalAngle, maxVerticalAngle);
    }

    private void Update()
    {
        target.rotation = Quaternion.Euler(kinematicsLayer.frame.look.y, kinematicsLayer.frame.look.x, 0);
    }

    private void OnDisable()
    {
        if (cursorController == this && cursorLocked)
        {
            //Unlock cursor
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        //Unhook
        kinematicsLayer.PreMove -= UpdateLook;

        controlLook.performed -= BufferInput;
    }
}