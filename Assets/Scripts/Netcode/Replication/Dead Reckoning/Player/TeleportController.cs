using System;
using Unity.Netcode;
using UnityEngine;

public sealed class TeleportController : NetworkBehaviour
{
    [SerializeField] private CharacterKinematics kinematicsLayer;
    [SerializeField] private PlayerRollbackReplicator rollback;

#if UNITY_EDITOR
    [TestButton("Force teleport", nameof(_ManuallyTeleport), isActiveInEditor = false, isActiveAtRuntime = true, order = 1)]
    [SerializeField] [InspectorReadOnly(playing = AccessMode.ReadWrite)] private Vector3 teleportPos;
    [SerializeField] [InspectorReadOnly(playing = AccessMode.ReadWrite)] private Vector3 teleportVel;
    [SerializeField] [InspectorReadOnly(playing = AccessMode.ReadWrite)] private Vector2 teleportLook;
    private bool teleportPending = false;
#else
    private Vector3 teleportPos;
    private Vector3 teleportVel;
    private Vector2 teleportLook;
    private bool teleportPending = false;
#endif

    private void OnEnable()
    {
        if (kinematicsLayer == null) kinematicsLayer = GetComponentInParent<CharacterKinematics>();
        if (rollback == null) rollback = kinematicsLayer.GetComponent<PlayerRollbackReplicator>();

        kinematicsLayer.MoveStep -= ApplyPendingTeleportation;
        kinematicsLayer.MoveStep += ApplyPendingTeleportation;
    }

    private void OnDisable()
    {
        kinematicsLayer.MoveStep -= ApplyPendingTeleportation;
    }

    //FIXME this technically violates pure function requirement of PreMove, since it reads and writes to values that change at runtime!
    private void ApplyPendingTeleportation(ref PlayerPhysicsFrame frame, CharacterKinematics.StepMode mode)
    {
        if (teleportPending && mode == CharacterKinematics.StepMode.SimulateVerify)
        {
            teleportPending = false;

            frame.mode     = PlayerPhysicsFrame.Mode.Teleport;
            frame.position = teleportPos;
            frame.look     = teleportLook;
            frame.velocity = teleportVel;
        }
    }

    public void Teleport(Vector3 pos, Vector3? vel = null, Vector2? look = null) //TODO refactor to use PreMove instead!
    {
        if (!IsServer) throw new AccessViolationException("Server frame is authority! Can only teleport on serverside.");

        teleportPending = true;
        teleportPos = pos;
        teleportVel = vel ?? Vector3.zero;
        teleportLook = look ?? Vector2.zero;
    }

#if UNITY_EDITOR
    private void _ManuallyTeleport() => Teleport(teleportPos, vel: teleportVel, look: teleportLook);
#endif
}
