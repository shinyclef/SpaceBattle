using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

[UpdateInGroup(typeof(GameGroupLateSim))]
public class ClearTriggerInfoBufferSys : JobComponentSystem
{
    private EndSimulationEntityCommandBufferSystem endSimCB;

    protected override void OnCreate()
    {
        endSimCB = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    //[BurstCompile]
    private struct Job : IJobForEachWithEntity<HasTriggerInfoTag>
    {
        public EntityCommandBuffer.Concurrent EndSimCB;
        [ReadOnly] public BufferFromEntity<TriggerInfoBuf> TriggerInfoBufs;

        public void Execute(Entity entity, int index, [ReadOnly] ref HasTriggerInfoTag tag)
        {
            EndSimCB.RemoveComponent<HasTriggerInfoTag>(index, entity);
            TriggerInfoBufs[entity].Clear();
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var job = new Job
        {
            EndSimCB = endSimCB.CreateCommandBuffer().ToConcurrent(),
            TriggerInfoBufs = GetBufferFromEntity<TriggerInfoBuf>()
        };

        var jh = job.Schedule(this, inputDeps);
        endSimCB.AddJobHandleForProducer(jh);
        return jh;
    }
}