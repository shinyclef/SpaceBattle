using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

[UpdateInGroup(typeof(GameGroupPostPhysics))]
[UpdateAfter(typeof(TriggerSys))]
public class DamageHealthOnTriggerSys : JobComponentSystem
{
    private BeginInitializationEntityCommandBufferSystem cmdBufferSystem;

    protected override void OnCreate()
    {
        cmdBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
    }

    [BurstCompile]
    [RequireComponentTag(typeof(TriggerInfoTag))]
    private struct Job : IJobForEachWithEntity<Health>
    {
        public EntityCommandBuffer.Concurrent CommandBuffer;

        [ReadOnly] public ComponentDataFromEntity<DamageHealthOnTrigger> DamageHealthComps;
        [ReadOnly] public BufferFromEntity<TriggerInfoBuf> TriggerInfoBufs;

        public void Execute(Entity entity, int index, [ReadOnly] ref Health health)
        {
            DynamicBuffer<TriggerInfoBuf> infoBuf = TriggerInfoBufs[entity];
            for (int i = 0; i < infoBuf.Length; i++)
            {
                TriggerInfo info = infoBuf[i];
                if (DamageHealthComps.Exists(info.OtherEntity))
                {
                    DamageHealthOnTrigger damageComp = DamageHealthComps[info.OtherEntity];
                    health.Value -= damageComp.Value;
                    if (health.Value <= 0)
                    {
                        CommandBuffer.DestroyEntity(index, entity);
                    }

                    CommandBuffer.DestroyEntity(index, info.OtherEntity);
                }
            }
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        // ApplyDamageSystem is looking for Query: "TriggerInfo, HealthDamageTrigger".
        // For each found entity, it uses 'ComponentDataFromEntity<TriggerInfo>' to check if each trigger partner has Health Comp.

        var job = new Job
        {
            CommandBuffer = cmdBufferSystem.CreateCommandBuffer().ToConcurrent(),
            DamageHealthComps = GetComponentDataFromEntity<DamageHealthOnTrigger>(),
            TriggerInfoBufs = GetBufferFromEntity<TriggerInfoBuf>(true)
        };

        JobHandle jh = job.Schedule(this, inputDeps);
        return jh;
    }
}