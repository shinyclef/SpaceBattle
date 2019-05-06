using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

[UpdateInGroup(typeof(MainGameGroup))]
public class LifeTimeExpireSys : JobComponentSystem
{
    private BeginInitializationEntityCommandBufferSystem cmdBufferSystem;

    protected override void OnCreate()
    {
        cmdBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
    }

    [BurstCompile]
    private struct Job : IJobForEachWithEntity<SpawnTime, LifeTime>
    {
        public EntityCommandBuffer.Concurrent CommandBuffer;
        public float Time;

        public void Execute(Entity entity, int index, [ReadOnly] ref SpawnTime st, [ReadOnly] ref LifeTime lt)
        {
            bool expired = Time - st.Value > lt.Value;
            if (expired)
            {
                CommandBuffer.DestroyEntity(index, entity);
            }
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var job = new Job()
        {
            CommandBuffer = cmdBufferSystem.CreateCommandBuffer().ToConcurrent(),
            Time = Time.time
        };

        JobHandle jh = job.Schedule(this, inputDeps);
        cmdBufferSystem.AddJobHandleForProducer(jh);
        return jh;
    }
}