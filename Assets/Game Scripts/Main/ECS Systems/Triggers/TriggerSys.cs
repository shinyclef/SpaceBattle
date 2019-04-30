using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;

[UpdateInGroup(typeof(GameGroupPostPhysics))]
[UpdateAfter(typeof(CombatTargetSys))]
public class TriggerSys : ComponentSystem
{
    private BuildPhysicsWorld buildPhysicsWorldSys;
    private StepPhysicsWorld stepPhysicsWorldSys;
    private Dictionary<Entity, List<TriggerInfo>> triggersMap;
    private EntityQuery handleTriggerQuery;

    protected override void OnCreate()
    {
        buildPhysicsWorldSys = World.GetOrCreateSystem<BuildPhysicsWorld>();
        stepPhysicsWorldSys = World.GetOrCreateSystem<StepPhysicsWorld>();
        triggersMap = new Dictionary<Entity, List<TriggerInfo>>();
        handleTriggerQuery = GetEntityQuery(typeof(HandleTriggersTag));
    }

    protected override void OnUpdate()
    {

        /* 1. Any entity that wants to know about colliding as triggers with things should have a HandleTriggers tag component.
         * --------------------
         * 2. TriggerSystem loops through Physics.TriggerEvents, creating a map of Entity -> TriggerInfo.
         * 3. Run query for all entities that have the TriggerInfo DynamicBuffer.
         * 4. For all found entities, add a "TriggerInfo" DynamicBuffer element and the TriggerInfoTag.
         * --------------------
         * 5. E.g. ApplyDamageSystem is looking for Query: "TriggerInfo, HullDamage".
         * 6. For each found entity, it uses 'ComponentDataFromEntity<HullHealth>' to check if each trigger partner has HullHealth Comp.
         * --------------------
         * 7. At the end of each frame, "ClearTriggerInfoBufferSys" runs, removing all "TriggerInfo" buffers by searching for the tag.
         * 
         * This scheme allows:
         * - One time looping through Physics.TriggerEvents
         * - Fast frame by frame checks for archetypes with TriggerInfo
         * - 'Opt-in' style trigger handlers
         */

        PrepareTriggerMap();

        NativeArray<Entity> triggerHandlerEntities = handleTriggerQuery.ToEntityArray(Allocator.TempJob);
        EntityManager em = World.Active.EntityManager;
        foreach (KeyValuePair<Entity, List<TriggerInfo>> map in triggersMap)
        {
            if (!em.HasComponent(map.Key, typeof(HandleTriggersTag)))
            {
                continue;
            }

            em.AddComponent(map.Key, typeof(TriggerInfoTag));
            DynamicBuffer<TriggerInfoBuf> buffer = em.GetBuffer<TriggerInfoBuf>(map.Key);
            foreach (TriggerInfo info in map.Value)
            {
                buffer.Add(info);
            }
        }

        triggerHandlerEntities.Dispose();
    }

    private void PrepareTriggerMap()
    {
        PhysicsWorld physicsWorld = buildPhysicsWorldSys.PhysicsWorld;
        stepPhysicsWorldSys.FinalJobHandle.Complete();
        TriggerEvents triggerEvents = stepPhysicsWorldSys.Simulation.TriggerEvents;

        NativeSlice<RigidBody> rbs = physicsWorld.Bodies;
        List<TriggerInfo> valueList;

        triggersMap.Clear();
        foreach (TriggerEvent trigger in triggerEvents)
        {
            Entity a = rbs[trigger.BodyIndices.BodyAIndex].Entity;
            Entity b = rbs[trigger.BodyIndices.BodyBIndex].Entity;

            var eventA = new TriggerInfo(b);
            var eventB = new TriggerInfo(a);

            if (!triggersMap.TryGetValue(a, out valueList))
            {
                valueList = new List<TriggerInfo>();
                triggersMap.Add(a, valueList);
            }

            valueList.Add(eventA);

            if (!triggersMap.TryGetValue(b, out valueList))
            {
                valueList = new List<TriggerInfo>();
                triggersMap.Add(b, valueList);
            }

            valueList.Add(eventB);
        }
    }
}