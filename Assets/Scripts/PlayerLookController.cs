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

    private void OnEnable()
    {
        //Hook
        kinematicsLayer.PreMove -= UpdateLook;
        kinematicsLayer.PreMove += UpdateLook;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsLocalPlayer) return;

        //Auto capture
        controlLook = dataSource.currentActionMap.FindAction(controlLookName);
        Debug.Assert(controlLook != null);

        //Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void UpdateLook(ref PlayerPhysicsFrame frame, CharacterKinematics.StepMode mode)
    {
        //Add given movement
        if (mode == CharacterKinematics.StepMode.LiveForward) frame.look += controlLook.ReadValue<Vector2>() * sensitivity * Time.deltaTime;

        //Limit
        frame.look.y = Mathf.Clamp(frame.look.y, minVerticalAngle, maxVerticalAngle);

        //Apply to transform
        if (mode == CharacterKinematics.StepMode.LiveForward
         || mode == CharacterKinematics.StepMode.LiveSpeculation) target.rotation = Quaternion.Euler(frame.look.y, frame.look.x, 0);
    }


    private void OnDisable()
    {
        if (!IsLocalPlayer) return;

        //Unlock cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        //Unhook
        kinematicsLayer.PreMove -= UpdateLook;
    }
}