using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

[UpdateInGroup(typeof(MainGameGroup))]
public class DamageHealthOnTriggerSys : JobComponentSystem
{
    private EndSimulationEntityCommandBufferSystem endSimCB;

    protected override void OnCreate()
    {
        endSimCB = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        // ApplyDamageSystem is looking for Query: "TriggerInfo, HealthDamageTrigger".
        // For each found entity, it uses 'ComponentDataFromEntity<TriggerInfo>' to check if each trigger partner has Health Comp.

        var job = new Job
        {
            EndSimCB = endSimCB.CreateCommandBuffer().ToConcurrent(),
            DamageHealthComps = GetComponentDataFromEntity<DamageHealthOnTrigger>()
        };

        World.GetExistingSystem<ProcessTriggerEventsSys>().FinalJobHandle.Complete();
        JobHandle jh = job.Schedule(this, inputDeps);
        endSimCB.AddJobHandleForProducer(jh);
        return jh;
    }

    [BurstCompile]
    [RequireComponentTag(typeof(HasTriggerInfoTag))]
    private struct Job : IJobForEachWithEntity_EBC<TriggerInfoBuf, Health>
    {
        public EntityCommandBuffer.Concurrent EndSimCB;
        [ReadOnly] public ComponentDataFromEntity<DamageHealthOnTrigger> DamageHealthComps;

        public void Execute(Entity entity, int index, [ReadOnly] DynamicBuffer<TriggerInfoBuf> infoBuf, [ReadOnly] ref Health health)
        {
            for (int i = 0; i < infoBuf.Length; i++)
            {
                TriggerInfo info = infoBuf[i];
                if (DamageHealthComps.Exists(info.OtherEntity))
                {
                    DamageHealthOnTrigger damageComp = DamageHealthComps[info.OtherEntity];
                    health.Value -= damageComp.Value;
                    if (health.Value <= 0)
                    {
                        EndSimCB.DestroyEntity(index, entity);
                    }

                    EndSimCB.DestroyEntity(index, info.OtherEntity);
                }
            }
        }
    }
}