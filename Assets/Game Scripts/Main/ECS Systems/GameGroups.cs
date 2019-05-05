using Unity.Entities;
using Unity.Physics.Systems;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(ExportPhysicsWorld))]
public class GameGroupPostPhysics : ComponentSystemGroup
{
    TriggerSys TriggerSys;
    LifeTimeExpireSys LifeTimeExpireSys;
    ShipSpawnerSys ShipSpawnerSys;
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
    ClearTriggerInfoBufferSys ClearTriggerInfoBufferSys;
}

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
public class GameGroupLateSim : ComponentSystemGroup
{
}