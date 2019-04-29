using Unity.Entities;
using Unity.Physics.Systems;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public class GameGroupPrePhysics : ComponentSystemGroup
{
    LifeTimeExpireSys LifeTimeExpireSys;
    ShipSpawnerSys ShipSpawnerSys;
    RotationSys RotationSys;
    VelocitySys VelocitySys;
    MovementSys MovementSys;
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(ExportPhysicsWorld))]
public class GameGroupPostPhysics : ComponentSystemGroup
{
    TriggerSys TriggerSys;
    DamageHealthOnTriggerSys DamageHealthOnTriggerSys;
    WeaponSys WeaponSys;
}