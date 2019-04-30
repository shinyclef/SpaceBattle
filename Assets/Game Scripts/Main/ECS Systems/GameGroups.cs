using Unity.Entities;
using Unity.Physics.Systems;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public class GameGroupPrePhysics : ComponentSystemGroup
{
    LifeTimeExpireSys LifeTimeExpireSys;
    ShipSpawnerSys ShipSpawnerSys;
    HeadingSys HeadingSys;
    AngularVelocitySys AngularVelocitySys;
    VelocitySys VelocitySys;
    RotationSys RotationSys;
    MovementSys MovementSys;
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(ExportPhysicsWorld))]
public class GameGroupPostPhysics : ComponentSystemGroup
{
    NearestEnemySys NearestEnemySys;
    CombatTargetSys TargetSys;
    TriggerSys TriggerSys;
    DamageHealthOnTriggerSys DamageHealthOnTriggerSys;
    WeaponSys WeaponSys;
}

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
public class GameGroupLateSim : ComponentSystemGroup
{
    ClearTriggerInfoBufferSys ClearTriggerInfoBufferSys;
}