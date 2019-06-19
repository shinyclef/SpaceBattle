using Unity.Burst;
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

    [BurstCompile]
    private struct FireWeaponJob : IJobForEachWithEntity<MoveDestination, LocalToWorld, CombatTarget, PhysicsVelocity, Weapon>
    {
        public NativeQueue<ProjectileSpawnData>.Concurrent Projectiles;
        [ReadOnly] public ComponentDataFromEntity<PhysicsVelocity> PhysicsVelocityData;
        [ReadOnly] public ComponentDataFromEntity<Velocity> VelocityData;
        public float Time;
        public float FixedDt;

        public void Execute(Entity entity, int index,
            [ReadOnly] ref MoveDestination moveDest,
            [ReadOnly] ref LocalToWorld l2w,
            [ReadOnly] ref CombatTarget target,
            [ReadOnly] ref PhysicsVelocity vel,
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

            bool isBursting = wep.FireBurstCount > 0 && wep.LastBurstShot > 0 && Time < wep.CooldownEnd;
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
                if (gmath.AngleBetweenVectors(targetDir, l2w.Up.xy) < wep.FireArcDegreesFromCenter)
                {
                    if (math.distance(l2w.Position.xy, moveDest.Value) < wep.ProjectileRange * 0.9f)
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
                float3 pos = l2w.Position + l2w.Up;
                float2 dir = math.normalizesafe(moveDest.Value - l2w.Position.xy);
                float angleToTarg = gmath.AngleBetweenVectorsSigned(l2w.Up.xy, dir);
                if (math.abs(angleToTarg) > wep.FireArcDegreesFromCenter)
                {
                    dir = gmath.RotateVector(l2w.Up.xy, wep.FireArcDegreesFromCenter * math.sign(angleToTarg));
                }

                ProjectileSpawnData data = new ProjectileSpawnData
                {
                    PrefabEntity = wep.ProjectilePrefab,
                    Pos = pos.xy,
                    Rot = quaternion.EulerXYZ(new float3(dir, 0)),
                    Velocity = dir.xy * VelocityData[wep.ProjectilePrefab].Speed + vel.Linear.xy
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
                BeginSimCB.SetComponent(proj, new Velocity { Value = data.Velocity });
            }
        }
    }

    private struct ProjectileSpawnData
    {
        public Entity PrefabEntity;
        public float2 Pos;
        public quaternion Rot;
        public float2 Velocity;
    }
}