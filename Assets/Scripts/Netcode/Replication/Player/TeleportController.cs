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
    [SerializeField] [InspectorReadOnly] private PlayerRollbackReplicator.OverwriteFlags pending = 0;
#else
    private Vector3 teleportPos;
    private Vector3 teleportVel;
    private Vector2 teleportLook;
    private PlayerRollbackReplicator.OverwriteFlags pending = 0;
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
    private void ApplyPendingTeleportation(ref PlayerPhysicsFrame frame, float dt, CharacterKinematics.StepMode mode)
    {
        if (mode == CharacterKinematics.StepMode.SimulateVerify)
        {
            frame.type     = PlayerPhysicsFrame.Type.Teleport;
            
            if(pending.HasFlag(PlayerRollbackReplicator.OverwriteFlags.Position)) frame.position = teleportPos;
            if(pending.HasFlag(PlayerRollbackReplicator.OverwriteFlags.Velocity)) frame.velocity = teleportVel;
            if(pending.HasFlag(PlayerRollbackReplicator.OverwriteFlags.Look    )) frame.look     = teleportLook;

            pending = 0;
        }
    }

    public void Teleport(Vector3? pos = null, Vector3? vel = null, Vector2? look = null) //TODO refactor to use PreMove instead!
    {
        if (!IsServer) throw new AccessViolationException("Server frame is authority! Can only teleport on serverside.");

        if (pos .HasValue) { pending |= PlayerRollbackReplicator.OverwriteFlags.Position; teleportPos  = pos .Value; }
        if (vel .HasValue) { pending |= PlayerRollbackReplicator.OverwriteFlags.Velocity; teleportVel  = vel .Value; }
        if (look.HasValue) { pending |= PlayerRollbackReplicator.OverwriteFlags.Look    ; teleportLook = look.Value; }
    }

#if UNITY_EDITOR
    private void _ManuallyTeleport() => Teleport(pos: teleportPos, vel: teleportVel, look: teleportLook);
#endif
}
