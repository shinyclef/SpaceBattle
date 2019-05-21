using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;

[UpdateInGroup(typeof(TriggerGameGroup))]
public class TriggerInfoPrepareSys : ComponentSystem
{
    private BuildPhysicsWorld buildPhysicsWorldSys;
    private StepPhysicsWorld stepPhysicsWorldSys;
    private HashSet<Entity> foundEntities;

    public bool DisposeRequired { get; private set; }
    public NativeMultiHashMap<Entity, TriggerInfo> TriggerMap { get; private set; }
    public NativeArray<Entity> TriggerKeys { get; private set; }

    protected override void OnCreate()
    {
        buildPhysicsWorldSys = World.GetOrCreateSystem<BuildPhysicsWorld>();
        stepPhysicsWorldSys = World.GetOrCreateSystem<StepPhysicsWorld>();
        foundEntities = new HashSet<Entity>();
    }

    protected override void OnUpdate()
    {
        EntityManager em = World.Active.EntityManager;
        PhysicsWorld physicsWorld = buildPhysicsWorldSys.PhysicsWorld;
        stepPhysicsWorldSys.FinalJobHandle.Complete();
        TriggerEvents triggerEvents = stepPhysicsWorldSys.Simulation.TriggerEvents;

        NativeSlice<RigidBody> rbs = physicsWorld.Bodies;
        foundEntities.Clear();

        foreach (var te in triggerEvents)
        {
            Entity a = rbs[te.BodyIndices.BodyAIndex].Entity;
            Entity b = rbs[te.BodyIndices.BodyBIndex].Entity;
            foundEntities.Add(a);
            foundEntities.Add(b);
        }

        if (foundEntities.Count == 0)
        {
            DisposeRequired = false;
            return;
        }

        TriggerMap = new NativeMultiHashMap<Entity, TriggerInfo>(foundEntities.Count, Allocator.TempJob);
        foreach (TriggerEvent trigger in triggerEvents)
        {
            Entity a = rbs[trigger.BodyIndices.BodyAIndex].Entity;
            Entity b = rbs[trigger.BodyIndices.BodyBIndex].Entity;

            if (em.HasComponent<HandleTriggersTag>(a))
            {
                var eventA = new TriggerInfo(b);
                TriggerMap.Add(a, eventA);
            }

            if (em.HasComponent<HandleTriggersTag>(b))
            {
                var eventB = new TriggerInfo(a);
                TriggerMap.Add(b, eventB);
            }
        }

        foundEntities.Clear();

        TriggerKeys = TriggerMap.GetKeyArray(Allocator.TempJob);
        em.AddComponent(TriggerKeys, typeof(HasTriggerInfoTag));
        DisposeRequired = true;
    }
}