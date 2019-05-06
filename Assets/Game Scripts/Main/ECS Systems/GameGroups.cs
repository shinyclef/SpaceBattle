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
    ShipSpawnerSpawnSys ShipSpawnerReclaimSys;
    ShipSpawnerSpawnSys ShipSpawnerSpawnSys;
    HeadingSys HeadingSys;
    //AngularVelocitySys AngularVelocitySys;
    RotationSys RotationSys;
    VelocitySys VelocitySys;
    MovementSys MovementSys;
    NearestEnemySys NearestEnemySys;
    CombatTargetSys CombatTargetSys;
    MoveDestinationSys MoveDestinationSys;
    DamageHealthOnTriggerSys DamageHealthOnTriggerSys;
    WeaponSys WeaponSys;
}

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
public class GameGroupLateSim : ComponentSystemGroup
{
    ClearTriggerInfoBufferSys ClearTriggerInfoBufferSys;
}