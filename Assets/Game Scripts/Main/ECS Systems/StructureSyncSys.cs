using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
[UpdateAfter(typeof(LateSimGameGroup))]
[AlwaysUpdateSystem]
public class StructureSyncSys : ComponentSystem
{
    private EntityQuery destroyedShipsQuery;
    private EntityQuery hasTriggerInfoTagQuery;

    private NativeQueue<ProjectileSpawnData> projectiles;
    private JobHandle dependencies;

    public NativeQueue<ProjectileSpawnData> Projectiles { get { return projectiles; } }

    protected override void OnCreate()
    {
        projectiles = new NativeQueue<ProjectileSpawnData>(Allocator.Persistent);
        destroyedShipsQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(ShipSpawnerOwnerSsShC) },
            None = new ComponentType[] { typeof(SpawnTime) }
        });

        hasTriggerInfoTagQuery = GetEntityQuery(typeof(HasTriggerInfoTag));
    }

    protected override void OnDestroy()
    {
        if (projectiles.IsCreated)
        {
            projectiles.Dispose();
        }
    }

    public void AddJobHandleForProducer(JobHandle producerJob)
    {
        dependencies = JobHandle.CombineDependencies(dependencies, producerJob);
    }

    protected override void OnUpdate()
    {
        dependencies.Complete();
        dependencies = new JobHandle();

        if (projectiles.Count > 0)
        {
            while (projectiles.TryDequeue(out ProjectileSpawnData data))
            {
                Entity e = EntityManager.Instantiate(data.PrefabEntity);
                EntityManager.SetComponentData(e, new Translation { Value = new float3(data.Pos, 0f) });
                EntityManager.SetComponentData(e, new Rotation { Value = data.Rot });
                EntityManager.SetComponentData(e, new SpawnTime { Value = Time.time });
            }
        }

        // 2. Remove destroyed ship system shared components
        World.Active.EntityManager.RemoveComponent(destroyedShipsQuery, typeof(ShipSpawnerOwnerSsShC));

        // 3. Remove trigger buffers
        World.Active.EntityManager.RemoveComponent(hasTriggerInfoTagQuery, typeof(HasTriggerInfoTag));
    }
}

public struct ProjectileSpawnData
{
    public Entity PrefabEntity;
    public float2 Pos;
    public quaternion Rot;
}