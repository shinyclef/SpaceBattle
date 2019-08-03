using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Physics;
using Unity.Physics.Systems;

[UpdateInGroup(typeof(PhysicsGameGroup))]
[AlwaysUpdateSystem]
public class ProcessTriggerEventsSys : JobComponentSystem
{
    private BuildPhysicsWorld buildPhysicsWorldSys;
    private StepPhysicsWorld stepPhysicsWorldSys;
    private EntityQuery triggerReceiversQuery;
    private NativeArray<int> counts;
    private NativeMultiHashMap<Entity, TriggerInfo> map;
    private NativeHashMap<Entity, bool> uniqueKeys;
    private bool firstIterationSkipped = false;

    public JobHandle FinalJobHandle { get; private set; }

    protected override void OnCreate()
    {
        buildPhysicsWorldSys = World.GetOrCreateSystem<BuildPhysicsWorld>();
        stepPhysicsWorldSys = World.GetOrCreateSystem<StepPhysicsWorld>();
        triggerReceiversQuery = GetEntityQuery(typeof(HasTriggerInfoTag), typeof(HandleTriggersTag), typeof(TriggerInfoBuf));
        counts = new NativeArray<int>(JobsUtility.MaxJobThreadCount, Allocator.Persistent);
        for (int i = 0; i < JobsUtility.MaxJobThreadCount; i++)
        {
            counts[i] = 0;
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        // TODO: Can sync points be removed?

        if (!firstIterationSkipped)
        {
            firstIterationSkipped = true;
            return inputDeps;
        }

        if (map.IsCreated)
        {
            map.Dispose();
            uniqueKeys.Dispose();
        }

        inputDeps = JobHandle.CombineDependencies(stepPhysicsWorldSys.FinalJobHandle, inputDeps);
        inputDeps = new CountJob
        {
            Counts = counts
        }.Schedule(stepPhysicsWorldSys.Simulation, ref buildPhysicsWorldSys.PhysicsWorld, inputDeps);

        inputDeps.Complete();

        int count = 0;
        for (int i = 0; i < JobsUtility.MaxJobThreadCount; i++)
        {
            count += counts[i];
            counts[i] = 0;
        }

        if (count == 0)
        {
            return inputDeps;
        }
        
        map = new NativeMultiHashMap<Entity, TriggerInfo>(count * 2, Allocator.TempJob);
        uniqueKeys = new NativeHashMap<Entity, bool>(count * 2, Allocator.TempJob);

        inputDeps = new PopulateTriggerMapJob
        {
            TriggerMap = map.AsParallelWriter(),
            UniqueKeys = uniqueKeys.AsParallelWriter(),
            HandleTriggersTagComps = GetComponentDataFromEntity<HandleTriggersTag>(true)
        }.Schedule(stepPhysicsWorldSys.Simulation, ref buildPhysicsWorldSys.PhysicsWorld, inputDeps);

        inputDeps.Complete();
        NativeArray<Entity> entities = uniqueKeys.GetKeyArray(Allocator.TempJob);
        EntityManager.AddComponent(entities, typeof(HasTriggerInfoTag));

        inputDeps = new AddBufferDataJob
        {
            TriggerMap = map
        }.Schedule(triggerReceiversQuery, inputDeps);

        entities.Dispose();
        FinalJobHandle = inputDeps;
        return inputDeps;
    }

    protected override void OnDestroy()
    {
        if (map.IsCreated)
        {
            map.Dispose();
            uniqueKeys.Dispose();
        }

        if (counts.IsCreated)
        {
            counts.Dispose();
        }
    }

    [BurstCompile]
    private struct CountJob : ITriggerEventsJob
    {
        [NativeDisableContainerSafetyRestriction] public NativeArray<int> Counts;
        #pragma warning disable 0649
        [NativeSetThreadIndex] private int threadId;
        #pragma warning restore 0649

        public void Execute(TriggerEvent triggerEvent)
        {
            Counts[threadId] = Counts[threadId] + 1;
        }
    }

    [BurstCompile]
    private struct PopulateTriggerMapJob : ITriggerEventsJob
    {
        public NativeMultiHashMap<Entity, TriggerInfo>.ParallelWriter TriggerMap;
        public NativeHashMap<Entity, bool>.ParallelWriter UniqueKeys;
        [ReadOnly] public ComponentDataFromEntity<HandleTriggersTag> HandleTriggersTagComps;

        public void Execute(TriggerEvent triggerEvent)
        {
            Entity a = triggerEvent.Entities.EntityA;
            Entity b = triggerEvent.Entities.EntityB;

            if (HandleTriggersTagComps.Exists(a))
            {
                TriggerMap.Add(a, new TriggerInfo(b));
                UniqueKeys.TryAdd(a, false);
            }

            if (HandleTriggersTagComps.Exists(b))
            {
                TriggerMap.Add(b, new TriggerInfo(a));
                UniqueKeys.TryAdd(b, false);
            }
        }
    }

    [BurstCompile]
    private struct AddBufferDataJob : IJobForEachWithEntity_EB<TriggerInfoBuf>
    {
        [NativeDisableParallelForRestriction] [ReadOnly] public NativeMultiHashMap<Entity, TriggerInfo> TriggerMap;

        public void Execute(Entity entity, int index, [WriteOnly] DynamicBuffer<TriggerInfoBuf> buffer)
        {
            if (TriggerMap.TryGetFirstValue(entity, out TriggerInfo info, out NativeMultiHashMapIterator<Entity> iterator))
            {
                do
                {
                    buffer.Add(info);
                }
                while (TriggerMap.TryGetNextValue(out info, ref iterator));
            }
        }
    }
}