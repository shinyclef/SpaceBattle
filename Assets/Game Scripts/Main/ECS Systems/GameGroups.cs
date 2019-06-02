using Unity.Entities;
using Unity.Physics.Systems;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public class InitializationGameGroup : ComponentSystemGroup
{
    AiDataSys AiLoadSys;
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(ExportPhysicsWorld))]
public class PhysicsGameGroup : ComponentSystemGroup
{
    ProcessTriggerEventsSys ProcessTriggerEventsSys;
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(PhysicsGameGroup))]
public class MainGameGroup : ComponentSystemGroup
{
    NearestEnemySys NearestEnemySys;
    LifeTimeExpireSys LifeTimeExpireSys;
    HeadingSys HeadingSys;
    RotationSys RotationSys;
    VelocitySys VelocitySys;
    MovementSys MovementSys;
    CombatTargetSys CombatTargetSys;
    CombatMovementAiSys CombatMovementAiSys;
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
    DeselectionSys DeselectionSys;
    SelectionSys SelectionSys;
} 