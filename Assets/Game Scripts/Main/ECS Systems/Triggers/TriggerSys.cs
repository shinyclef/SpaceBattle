using System.Collections.Generic;
using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

[UpdateInGroup(typeof(GameGroupPostPhysics))]
public class TriggerSys : JobComponentSystem
{
    //private BeginInitializationEntityCommandBufferSystem beginSimCB;
    private BuildPhysicsWorld buildPhysicsWorldSys;
    private StepPhysicsWorld stepPhysicsWorldSys;
    private HashSet<Entity> foundEntities;

    protected override void OnCreate()
    {
        //beginSimCB = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        buildPhysicsWorldSys = World.GetOrCreateSystem<BuildPhysicsWorld>();
        stepPhysicsWorldSys = World.GetOrCreateSystem<StepPhysicsWorld>();
        foundEntities = new HashSet<Entity>();
    }

    //[BurstCompile]
    //private struct PrepareTriggerMapJob : IJob
    //{
    //    [ReadOnly] public PhysicsWorld PhysicsWorld;
    //    [ReadOnly] public TriggerEvents TriggerEvents;
    //    public NativeMultiHashMap<Entity, TriggerInfo> TriggerMap;

    //    public void Execute()
    //    {
    //        NativeSlice<RigidBody> rbs = PhysicsWorld.Bodies;
    //        foreach (TriggerEvent trigger in TriggerEvents)
    //        {
    //            Entity a = rbs[trigger.BodyIndices.BodyAIndex].Entity;
    //            Entity b = rbs[trigger.BodyIndices.BodyBIndex].Entity;

    //            var eventA = new TriggerInfo(b);
    //            var eventB = new TriggerInfo(a);

    //            TriggerMap.Add(a, eventA);
    //            TriggerMap.Add(b, eventB);
    //        }
    //    }
    //}

    [BurstCompile]
    private struct AddTriggerComponentsJob : IJobForEachWithEntity<HandleTriggersTag>
    {
        [ReadOnly] public NativeMultiHashMap<Entity, TriggerInfo> TriggerMap;
        [NativeDisableParallelForRestriction] public BufferFromEntity<TriggerInfoBuf> TriggerInfoBufs;

        public void Execute(Entity entity, int index, [ReadOnly] ref HandleTriggersTag dummy)
        {
            NativeArray<TriggerInfo> infoArr = TriggerMap.GetValueArray(Allocator.Temp);
            DynamicBuffer<TriggerInfoBuf> buffer = TriggerInfoBufs[entity];
            for (int j = 0; j < infoArr.Length; j++)
            {
                buffer.Add(infoArr[j]);
            }

            infoArr.Dispose();
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        #region notes
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
        #endregion

        //PrepareTriggerMap();

        var sw = new Stopwatch();
        sw.Start();

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

        NativeMultiHashMap<Entity, TriggerInfo> triggerMap = new NativeMultiHashMap<Entity, TriggerInfo>(foundEntities.Count, Allocator.TempJob);
        foreach (TriggerEvent trigger in triggerEvents)
        {
            Entity a = rbs[trigger.BodyIndices.BodyAIndex].Entity;
            Entity b = rbs[trigger.BodyIndices.BodyBIndex].Entity;

            if (em.HasComponent<HandleTriggersTag>(a))
            {
                var eventA = new TriggerInfo(b);
                triggerMap.Add(a, eventA);
            }

            if (em.HasComponent<HandleTriggersTag>(b))
            {
                var eventB = new TriggerInfo(a);
                triggerMap.Add(b, eventB);
            }
        }

        //var job = new PrepareTriggerMapJob
        //{
        //    PhysicsWorld = physicsWorld,
        //    TriggerEvents = triggerEvents,
        //    TriggerMap = triggerMap
        //};

        //JobHandle jh = job.Schedule();
        //jh.Complete();


        NativeArray<Entity> triggerKeys = triggerMap.GetKeyArray(Allocator.TempJob);
        
        

        em.AddComponent(triggerKeys, typeof(HasTriggerInfoTag));
        var addComponentsJob = new AddTriggerComponentsJob
        {
            //BeginSimCB = beginSimCB.CreateCommandBuffer().ToConcurrent(),
            TriggerMap = triggerMap,
            TriggerInfoBufs = GetBufferFromEntity<TriggerInfoBuf>()
        };

        EntityQuery query = GetEntityQuery(typeof(HasTriggerInfoTag), typeof(HandleTriggersTag));
        JobHandle jh = addComponentsJob.Schedule(query, inputDeps);
        jh.Complete();

        triggerMap.Dispose();
        triggerKeys.Dispose();

        sw.Stop();
        //Logger.Log("Time: " + sw.Elapsed.TotalMilliseconds);

        return jh;
    }
}