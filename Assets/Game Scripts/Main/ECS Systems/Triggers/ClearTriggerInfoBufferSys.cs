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

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        EntityManager em = World.Active.EntityManager;
        EntityQuery q = GetEntityQuery(typeof(HasTriggerInfoTag));
        NativeArray<Entity> entities = q.ToEntityArray(Allocator.TempJob);
        for (int i = 0; i < entities.Length; i++)
        {
            em.GetBuffer<TriggerInfoBuf>(entities[i]).Clear();
        }

        em.RemoveComponent(q, typeof(HasTriggerInfoTag));
        entities.Dispose();
        return inputDeps;

        var job = new Job
        {
            EndSimCB = endSimCB.CreateCommandBuffer().ToConcurrent(),
            TriggerInfoBufs = GetBufferFromEntity<TriggerInfoBuf>()
        };

        var jh = job.Schedule(this, inputDeps);
        endSimCB.AddJobHandleForProducer(jh);
        return jh;
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
}