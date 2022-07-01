using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PlayerLookController : MonoBehaviour
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
    [SerializeField] private Vector2 angles;
    public Vector2 Angles => angles;

    private void Update()
    {
        //Add given movement
        angles += controlLook.ReadValue<Vector2>() * sensitivity * Time.deltaTime;

        //Limit
        angles.y = Mathf.Clamp(angles.y, minVerticalAngle, maxVerticalAngle);

        //Apply
        target.rotation = Quaternion.Euler(angles.y, angles.x, 0);
    }

    private void OnEnable()
    {
        //Auto capture
        controlLook = dataSource.currentActionMap.FindAction(controlLookName);

        //Lock cursor
        if (takeCursorControl)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void OnDisable()
    {
        //Unlock cursor
        if (takeCursorControl)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}