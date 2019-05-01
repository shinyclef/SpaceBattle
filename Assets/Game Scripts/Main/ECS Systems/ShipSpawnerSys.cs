using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

[UpdateInGroup(typeof(GameGroupPrePhysics))]
[UpdateAfter(typeof(LifeTimeExpireSys))]
public class ShipSpawnerSys : JobComponentSystem
{
    private BeginInitializationEntityCommandBufferSystem cmdBufferSystem;
    private EntityQuery destroyedShipsQuery;

    protected override void OnCreate()
    {
        cmdBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        destroyedShipsQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(ShipSpawnerOwnerSsShC) },
            None = new ComponentType[] { typeof(SpawnTime) }
        });
    }

    //[BurstCompile]
    private struct SpawnJob : IJobForEachWithEntity<ShipSpawner, LocalToWorld, Translation, Rotation>
    {
        public EntityCommandBuffer CommandBuffer;
        public Random Rand;
        public float Time;
        public float Dt;

        public void Execute(Entity entity, int index, ref ShipSpawner spawner, [ReadOnly] ref LocalToWorld location, [ReadOnly] ref Translation tran, [ReadOnly] ref Rotation rot)
        {
            int maxToSpawn = spawner.MaxShips - spawner.ActiveShipCount;
            float desiredSpawnCount = spawner.SpawnRatePerSecond * Dt + spawner.SpawnCountRemainder;
            int flooredSpawnCount = (int)desiredSpawnCount;
            spawner.SpawnCountRemainder = desiredSpawnCount - flooredSpawnCount;
            int spawnCount = math.min(flooredSpawnCount, maxToSpawn);
            spawner.ActiveShipCount += spawnCount;

            float3 ss = spawner.SpawnSpread;
            float heading = Heading.FromQuaternion(rot.Value);
            float2 moveDest = tran.Value.xy + Heading.ToFloat2(heading);
            for (int i = 0; i < spawnCount; i++)
            {
                float3 pos = math.transform(location.Value, new float3(Rand.NextFloat(-ss.x, ss.x),
                                                                       Rand.NextFloat(-ss.y, ss.y),
                                                                       Rand.NextFloat(-ss.z, ss.z)));

                Entity ship = CommandBuffer.Instantiate(spawner.ShipPrefab);
                CommandBuffer.SetComponent(ship, new Translation { Value = pos });
                CommandBuffer.SetComponent(ship, new Heading(heading));
                CommandBuffer.SetComponent(ship, new MoveDestination(moveDest));
                CommandBuffer.SetComponent(ship, new SpawnTime(Time));
                CommandBuffer.AddSharedComponent(ship, new ShipSpawnerOwnerSsShC(entity.Index, entity.Version));
            }
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (Time.frameCount < 20f)
        {
            return inputDeps;
        }
        
        // ------------------------------------------------------- //
        // For each spawner, decrement its 'ActiveShipCount' value //
        // ------------------------------------------------------- //

        // 1. Query all 'ShipSpawnerOwner' chunks
        var em = World.Active.EntityManager;
        var compType = GetArchetypeChunkSharedComponentType<ShipSpawnerOwnerSsShC>();

        // 2. Loop through summing up destroyed counts in a dictionary
        var destoryedShipCounts = new Dictionary<Entity, int>();

        using (NativeArray<ArchetypeChunk> chunks = destroyedShipsQuery.CreateArchetypeChunkArray(Allocator.TempJob))
        {
            foreach (ArchetypeChunk chunk in chunks)
            {
                ShipSpawnerOwnerSsShC owner = chunk.GetSharedComponentData(compType, em);
                Entity e = new Entity() { Index = owner.EntityIndex, Version = owner.EntityVer };
                int count = chunk.Count;
                if (!destoryedShipCounts.ContainsKey(e))
                {
                    destoryedShipCounts.Add(e, count);
                }
                else
                {
                    destoryedShipCounts[e] += count;
                }
            }

            // 3. Apply totals to components directly
            foreach (KeyValuePair<Entity, int> pair in destoryedShipCounts)
            {
                if (em.Exists(pair.Key))
                {
                    var spawner = em.GetComponentData<ShipSpawner>(pair.Key);
                    spawner.ActiveShipCount -= pair.Value;
                    em.SetComponentData(pair.Key, spawner);
                }
            }
        }

        // 4. Remove the system shared component
        em.RemoveComponent(destroyedShipsQuery, typeof(ShipSpawnerOwnerSsShC));

        // ---------------- //
        // Schedule the Job //
        // ---------------- //

        var job = new SpawnJob
        {
            CommandBuffer = cmdBufferSystem.CreateCommandBuffer(),
            Rand = new Random(Rand.New().NextUInt()),
            Time = Time.time,
            Dt = Time.deltaTime,
        };

        JobHandle jh = job.ScheduleSingle(this, inputDeps);
        cmdBufferSystem.AddJobHandleForProducer(jh);
        return jh;
    }
}