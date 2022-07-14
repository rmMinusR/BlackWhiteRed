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

    [Space]
    [InspectorReadOnly] [SerializeField] private Vector2 angles;

    [Header("Bindings")]
    [SerializeField] private CharacterKinematics kinematicsLayer;
    [SerializeField] private PlayerInput dataSource;
    private InputAction controlLook;
    [SerializeField] private string controlLookName = "Look";

    private void WriteLook(ref PlayerPhysicsFrame frame, bool live)
    {
        if (live) frame.look = angles;
    }

    private void Update()
    {
        if (IsLocalPlayer && IsSpawned)
        {
            //Add given movement
            angles += controlLook.ReadValue<Vector2>() * sensitivity * Time.deltaTime;

            //Limit
            angles.y = Mathf.Clamp(angles.y, minVerticalAngle, maxVerticalAngle);
        }

        target.rotation = Quaternion.Euler(angles.y, angles.x, 0);
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

        //Hook
        kinematicsLayer.PreMove -= WriteLook;
        kinematicsLayer.PreMove += WriteLook;
    }

    private void OnDisable()
    {
        if (!IsLocalPlayer) return;

        //Unlock cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        //Unhook
        kinematicsLayer.PreMove -= WriteLook;
    }
}