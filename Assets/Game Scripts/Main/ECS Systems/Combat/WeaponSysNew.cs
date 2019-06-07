using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(MainGameGroup))]
//[DisableAutoCreation]
public class WeaponSysNew : JobComponentSystem
{
    private StructureSyncSys spawnerSys;

    protected override void OnCreate()
    {
        spawnerSys = World.GetOrCreateSystem<StructureSyncSys>();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        inputDeps = new Job()
        {
            Projectiles = spawnerSys.Projectiles.ToConcurrent(),
            VelocityData = GetComponentDataFromEntity<Velocity>(),
            Time = Time.time
        }.Schedule(this, inputDeps);

        spawnerSys.AddJobHandleForProducer(inputDeps);
        return inputDeps;
    }

    [BurstCompile]
    private struct Job : IJobForEachWithEntity<MoveDestination, LocalToWorld, Rotation, Velocity, CombatTarget, Weapon>
    {
        public NativeQueue<ProjectileSpawnData>.Concurrent Projectiles;
        [ReadOnly] public ComponentDataFromEntity<Velocity> VelocityData;
        public float Time;

        public void Execute(Entity entity, int index,
            [ReadOnly] ref MoveDestination moveDest,
            [ReadOnly] ref LocalToWorld l2w,
            [ReadOnly] ref Rotation rot,
            [ReadOnly] ref Velocity vel,
            [ReadOnly] ref CombatTarget target,
            ref Weapon wep)
        {
            if (wep.CooldownEnd == 0)
            {
                wep.CooldownEnd = Time + wep.FireMajorInterval;
                wep.BurstShotCooldownEnd = wep.FireMinorInterval;
                return;
            }

            if (target.Entity == Entity.Null || !VelocityData.Exists(target.Entity))
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
            else if (Time < wep.BurstShotCooldownEnd)// in the middle of burst
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
                    float2 projectedEnemyPos = moveDest.Value + VelocityData[target.Entity].Value * wep.projectileLifeTime;
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
                    Rot = rot.Value
                };

                Projectiles.Enqueue(data);
            }
        }
    }
}