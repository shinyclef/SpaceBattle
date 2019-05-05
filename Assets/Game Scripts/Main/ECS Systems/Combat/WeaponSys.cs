using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(GameGroupPostPhysics))]
//[UpdateAfter(typeof(DamageHealthOnTriggerSys))]
public class WeaponSys : JobComponentSystem
{
    private BeginInitializationEntityCommandBufferSystem beingSimCB;

    protected override void OnCreate()
    {
        beingSimCB = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
    }

    //[BurstCompile]
    private struct Job : IJobForEachWithEntity<MoveDestination, Translation, Rotation, Velocity, CombatTarget, Weapon>
    {
        public EntityCommandBuffer.Concurrent BeginSimCB;
        [ReadOnly] public ComponentDataFromEntity<Velocity> VelocityData;
        public float Time;

        public void Execute(Entity entity, int index, 
            [ReadOnly] ref MoveDestination moveDest, 
            [ReadOnly] ref Translation tran, 
            [ReadOnly] ref Rotation rot,
            [ReadOnly] ref Velocity vel,
            [ReadOnly] ref CombatTarget target,
            ref Weapon wep)
        {
            if (wep.CooldownEnd == 0)
            {
                wep.CooldownEnd = Time + (Time * 10000 + index) % wep.FireInterval;
                return;
            }

            if (Time < wep.CooldownEnd || !moveDest.IsCombatTarget)
            {
                return;
            }
            
            float2 projectedEnemyPos = moveDest.Value + VelocityData[target.Value].Value * wep.projectileLifeTime;
            if (math.distance(tran.Value.xy, projectedEnemyPos) < wep.projectileRange)
            {
                wep.CooldownEnd = math.max(wep.CooldownEnd + wep.FireInterval, Time + wep.FireInterval - 0.1f);
                float3 pos = tran.Value + wep.SpawnOffset.LocalToWorldPos(rot.Value);
                Entity proj = BeginSimCB.Instantiate(index, wep.ProjectilePrefab);
                BeginSimCB.SetComponent(index, proj, new Translation { Value = pos });
                BeginSimCB.SetComponent(index, proj, new Rotation { Value = rot.Value });
                BeginSimCB.SetComponent(index, proj, new SpawnTime { Value = Time });
            }
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var job = new Job()
        {
            BeginSimCB = beingSimCB.CreateCommandBuffer().ToConcurrent(),
            VelocityData = GetComponentDataFromEntity<Velocity>(),
            Time = Time.time
        };

        JobHandle jh = job.Schedule(this, inputDeps);
        beingSimCB.AddJobHandleForProducer(jh);
        return jh;
    }
}