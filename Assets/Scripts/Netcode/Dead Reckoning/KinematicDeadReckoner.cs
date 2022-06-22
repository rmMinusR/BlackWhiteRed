using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class KinematicDeadReckoner : MonoBehaviour
{
    [SerializeField] private Rigidbody source;
    [SerializeField] private Transform target;
    private Rigidbody targetAsRb;
    private bool isSetupValid = false;
    //TODO add smoothing factor

    private void Start()
    {
        isSetupValid = true;

        // Try to capture components

        if (!source) source = GetComponent<Rigidbody>();
        isSetupValid &= source;
        Debug.Assert(source, "No source found, and could not find a substitute!", this);

        isSetupValid &= target;
        Debug.Assert(target, "No target found!", this);
        target.TryGetComponent(out targetAsRb);
    }

    private void FixedUpdate()
    {
        if (isSetupValid) // Only run if setup was valid
        {
            PhysicsFrame frame = PhysicsFrame.For(source);
            //frame.time = ???; //TODO use the time associated with networked variables' last FixedUpdate

            _SetTarget(DeadReckoningUtility.DeadReckon(frame, Time.fixedTime));
        }
    }

    /// <summary>
    /// Helper function to move the target using the most appropriate available method
    /// </summary>
    /// <param name="frame">Snapshot to apply. Does NOT account for time mismatch.</param>
    private void _SetTarget(PhysicsFrame frame)
    {
        if (targetAsRb)
        {
            targetAsRb.MovePosition(frame.position);
            targetAsRb.velocity = frame.velocity;
        }
        else transform.position = frame.position;
    }
}
