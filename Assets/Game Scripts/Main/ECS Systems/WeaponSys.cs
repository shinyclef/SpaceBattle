using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(GameGroupPostPhysics))]
[UpdateAfter(typeof(DamageHealthOnTriggerSys))]
public class WeaponSys : JobComponentSystem
{
    private BeginInitializationEntityCommandBufferSystem cmdBufferSystem;

    protected override void OnCreate()
    {
        cmdBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
    }

    //[BurstCompile]
    private struct Job : IJobForEachWithEntity<Weapon, Translation, Rotation>
    {
        public EntityCommandBuffer.Concurrent CommandBuffer;
        public float Time;

        public void Execute(Entity entity, int index, ref Weapon wep, [ReadOnly] ref Translation tran, [ReadOnly] ref Rotation rot)
        {
            if (wep.CooldownEnd == 0)
            {
                wep.CooldownEnd = (Time * 10000 + index) % wep.FireInterval;
            }
            else if (Time >= wep.CooldownEnd)
            {
                wep.CooldownEnd = math.max(wep.CooldownEnd + wep.FireInterval, Time);
                float3 pos = tran.Value + wep.SpawnOffset.LocalToWorldPos(rot.Value);
                Entity proj = CommandBuffer.Instantiate(index, wep.ProjectilePrefab);
                CommandBuffer.SetComponent(index, proj, new Translation { Value = pos });
                CommandBuffer.SetComponent(index, proj, new Rotation { Value = rot.Value });
                CommandBuffer.SetComponent(index, proj, new SpawnTime { Value = Time });
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