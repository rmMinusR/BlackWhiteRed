using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PlayerLookController : NetworkBehaviour
{
    [SerializeField] private PlayerInput dataSource;
    private InputAction controlLook;
    [SerializeField] private string controlLookName = "Look";

    [SerializeField] private bool takeCursorControl = true;

    [Space]
    [SerializeField] private Transform target;
    [SerializeField] private Vector2 sensitivity = Vector2.one;
    [SerializeField] [Range(-90,  0)] private float minVerticalAngle = -90;
    [SerializeField] [Range(  0, 90)] private float maxVerticalAngle = 90;

    [Space]
    [SerializeField] private NetworkVariable<Vector2> angles = new NetworkVariable<Vector2>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Owner);
    public Vector2 Angles => angles.Value;

    private void Update()
    {
        if (IsLocalPlayer && IsSpawned)
        {
            Vector2 newAngles = angles.Value;

            //Add given movement
            newAngles += controlLook.ReadValue<Vector2>() * sensitivity * Time.deltaTime;

            //Limit
            newAngles.y = Mathf.Clamp(newAngles.y, minVerticalAngle, maxVerticalAngle);

            //Apply
            angles.Value = newAngles;
        }

        target.rotation = Quaternion.Euler(Angles.y, Angles.x, 0);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsLocalPlayer) return;

        //Auto capture
        controlLook = dataSource.currentActionMap.FindAction(controlLookName);
        Debug.Assert(controlLook != null);

        //Lock cursor
        if (takeCursorControl)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void OnDisable()
    {
        if (!IsLocalPlayer) return;

        //Unlock cursor
        if (takeCursorControl)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}