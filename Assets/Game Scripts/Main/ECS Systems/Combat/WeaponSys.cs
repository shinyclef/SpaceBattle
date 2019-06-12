﻿using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

[AlwaysUpdateSystem]
[UpdateInGroup(typeof(MainGameGroup))]
public class WeaponSys : JobComponentSystem
{
    private BeginSimulationEntityCommandBufferSystem beginSimCB;
    private NativeQueue<ProjectileSpawnData> projectiles;

    protected override void OnCreate()
    {
        beginSimCB = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
    }

    protected override void OnDestroy()
    {
        Dispose();
    }

    private void Dispose()
    {
        if (projectiles.IsCreated)
        {
            projectiles.Dispose();
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        Dispose();
        projectiles = new NativeQueue<ProjectileSpawnData>(Allocator.TempJob);
        inputDeps = new FireWeaponJob()
        {
            Projectiles = projectiles.ToConcurrent(),
            PhysicsVelocityData = GetComponentDataFromEntity<PhysicsVelocity>(true),
            VelocityData = GetComponentDataFromEntity<Velocity>(true),
            Time = Time.time,
            FixedDt = Time.deltaTime // TODO: Correct Physics Timestep!!!
        }.Schedule(this, inputDeps);

        inputDeps = new PopulateCmdBufJob()
        {
            BeginSimCB = beginSimCB.CreateCommandBuffer(),
            Projectiles = projectiles,
            Time = Time.time
        }.Schedule(inputDeps);

        beginSimCB.AddJobHandleForProducer(inputDeps);
        return inputDeps;
    }

    //[BurstCompile]
    private struct FireWeaponJob : IJobForEachWithEntity<MoveDestination, LocalToWorld, Rotation, CombatTarget, Weapon>
    {
        public NativeQueue<ProjectileSpawnData>.Concurrent Projectiles;
        [ReadOnly] public ComponentDataFromEntity<PhysicsVelocity> PhysicsVelocityData;
        [ReadOnly] public ComponentDataFromEntity<Velocity> VelocityData;
        public float Time;
        public float FixedDt;

        public void Execute(Entity entity, int index,
            [ReadOnly] ref MoveDestination moveDest,
            [ReadOnly] ref LocalToWorld l2w,
            [ReadOnly] ref Rotation rot,
            [ReadOnly] ref CombatTarget target,
            ref Weapon wep)
        {
            if (wep.CooldownEnd == 0)
            {
                wep.CooldownEnd = Time + wep.FireMajorInterval;
                wep.BurstShotCooldownEnd = wep.FireMinorInterval;
                return;
            }

            if (target.Entity == Entity.Null || !PhysicsVelocityData.Exists(target.Entity))
            {
                wep.LastBurstShot = 0;
                return;
            }

            bool isBursting = wep.LastBurstShot > 0 && Time < wep.CooldownEnd;
            if (!isBursting)
            {
                if (Time < wep.CooldownEnd)
                {
                    return;
                }
            }
            else if (Time < wep.BurstShotCooldownEnd) // in the middle of burst
            {
                return;
            }

            bool fire = false;
            if (!isBursting)
            {
                float2 targetDir = math.normalize(target.Pos - l2w.Position.xy);
                float2 forwardDir = l2w.Up.xy;
                if (math.dot(targetDir, forwardDir) > 0.995f)
                {
                    float2 projectedEnemyPos = moveDest.Value + PhysicsVelocityData[target.Entity].Linear.xy * (wep.projectileLifeTime * 0.9f);
                    if (math.distance(l2w.Position.xy, projectedEnemyPos) < wep.projectileRange)
                    {
                        wep.CooldownEnd = math.max(wep.CooldownEnd + wep.FireMajorInterval, Time + wep.FireMajorInterval - 0.1f);
                        wep.BurstShotCooldownEnd = math.max(wep.BurstShotCooldownEnd + wep.FireMinorInterval, Time + wep.FireMinorInterval - 0.01f);
                        wep.LastBurstShot = 1;
                        fire = true;
                    }
                }
            }
            else
            {
                wep.BurstShotCooldownEnd = math.max(wep.BurstShotCooldownEnd + wep.FireMinorInterval, Time + wep.FireMinorInterval - 0.01f);
                wep.LastBurstShot = (wep.LastBurstShot + 1) % wep.FireBurstCount;
                fire = true;
            }

            if (fire)
            {
                float3 pos = l2w.Position + wep.SpawnOffset.LocalToWorldPos(rot.Value);
                ProjectileSpawnData data = new ProjectileSpawnData
                {
                    PrefabEntity = wep.ProjectilePrefab,
                    Pos = pos.xy,
                    Rot = rot.Value,
                    Speed = VelocityData[wep.ProjectilePrefab].Speed
                };

                Projectiles.Enqueue(data);
            }
        }
    }

    private struct PopulateCmdBufJob : IJob
    {
        public EntityCommandBuffer BeginSimCB;
        public NativeQueue<ProjectileSpawnData> Projectiles;
        public float Time;

        public void Execute()
        {
            while (Projectiles.TryDequeue(out ProjectileSpawnData data))
            {
                Entity proj = BeginSimCB.Instantiate(data.PrefabEntity);
                BeginSimCB.SetComponent(proj, new Translation { Value = new float3(data.Pos, 0f) });
                BeginSimCB.SetComponent(proj, new Rotation { Value = data.Rot });
                BeginSimCB.SetComponent(proj, new SpawnTime { Value = Time });
                BeginSimCB.SetComponent(proj, new Velocity { Value = data.Rot.Up().xy * data.Speed });
            }
        }
    }

    private struct ProjectileSpawnData
    {
        public Entity PrefabEntity;
        public float2 Pos;
        public quaternion Rot;
        public float Speed;
    }
}