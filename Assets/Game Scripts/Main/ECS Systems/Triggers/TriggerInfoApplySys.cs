using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics.Systems;

[UpdateInGroup(typeof(TriggerGameGroup))]
[UpdateAfter(typeof(TriggerInfoPrepareSys))]
public class TriggerInfoApplySys : JobComponentSystem
{
    private TriggerInfoPrepareSys triggerPrepSys;
    private StepPhysicsWorld stepPhysicsWorldSys;
    private EntityQuery triggerReceiversQuery;

    protected override void OnCreate()
    {
        triggerPrepSys = World.GetOrCreateSystem<TriggerInfoPrepareSys>();
        stepPhysicsWorldSys = World.GetOrCreateSystem<StepPhysicsWorld>();
        triggerReceiversQuery = GetEntityQuery(typeof(HasTriggerInfoTag), typeof(HandleTriggersTag));
    }

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
        stepPhysicsWorldSys.FinalJobHandle.Complete();

        var addComponentsJob = new AddTriggerComponentsJob
        {
            TriggerMap = triggerPrepSys.TriggerMap,
            TriggerInfoBufs = GetBufferFromEntity<TriggerInfoBuf>()
        };

        JobHandle jh = addComponentsJob.Schedule(triggerReceiversQuery, inputDeps);
        jh.Complete();
        return jh;
    }
}