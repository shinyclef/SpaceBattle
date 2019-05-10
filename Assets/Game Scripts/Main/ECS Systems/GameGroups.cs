using Unity.Entities;
using Unity.Physics.Systems;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(ExportPhysicsWorld))]
public class TriggerGameGroup : ComponentSystemGroup
{
    TriggerInfoPrepareSys TriggerInfoPrepareSys;
    TriggerInfoApplySys TriggerInfoApplySys;
    TriggerInfoNativeCleanupSys TriggerInfoNativeCleanupSys;
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(TriggerGameGroup))]
public class MainGameGroup : ComponentSystemGroup
{
    LifeTimeExpireSys LifeTimeExpireSys;
    HeadingSys HeadingSys;
    RotationSys RotationSys;
    VelocitySys VelocitySys;
    MovementSys MovementSys;
    NearestEnemySys NearestEnemySys;
    CombatTargetSys CombatTargetSys;
    CombatMovementAiSys CombatMovementAiSys;
    //MoveDestinationSys MoveDestinationSys;
    DamageHealthOnTriggerSys DamageHealthOnTriggerSys;
    WeaponSys WeaponSys;
}

/// <summary>
/// Ship spawning should occur just before transform systems so the LocalToWorld component is updated before use.
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(MainGameGroup))]
public class SpawnerGameGroup : ComponentSystemGroup
{
    ShipSpawnerReclaimSys ShipSpawnerReclaimSys;
    ShipSpawnerSpawnSys ShipSpawnerSpawnSys;
}

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
public class GameGroupLateSim : ComponentSystemGroup
{
    ClearTriggerInfoBufferSys ClearTriggerInfoBufferSys;
}