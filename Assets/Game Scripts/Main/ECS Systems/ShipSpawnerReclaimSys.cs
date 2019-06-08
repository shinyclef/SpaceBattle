using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

[UpdateInGroup(typeof(SpawnerGameGroup))]
[AlwaysUpdateSystem]
public class ShipSpawnerReclaimSys : JobComponentSystem
{
    private EntityQuery destroyedShipsQuery;
    private NativeHashMap<Entity, int> destroyedShipCounts;
    private NativeArray<ArchetypeChunk> chunks;
    private NativeArray<Entity> keys;
    private NativeArray<int> vals;

    protected override void OnCreate()
    {
        destroyedShipsQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(ShipSpawnerOwnerSsShC) },
            None = new ComponentType[] { typeof(SpawnTime) }
        });
    }

    protected override void OnDestroy()
    {
        Dispose(); 
    }

    private void Dispose()
    {
        if (destroyedShipCounts.IsCreated)
        {
            destroyedShipCounts.Dispose();
        }

        if (chunks.IsCreated)
        {
            chunks.Dispose();
        }

        if (keys.IsCreated)
        {
            keys.Dispose();
        }
        
        if (vals.IsCreated)
        {
            vals.Dispose();
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        Dispose();
        var em = World.Active.EntityManager;

        // 1. Loop through summing up destroyed counts
        int totalCount = destroyedShipsQuery.CalculateLength();
        if (totalCount == 0)
        {
            return inputDeps;
        }

        var compType = GetArchetypeChunkSharedComponentType<ShipSpawnerOwnerSsShC>();
        destroyedShipCounts = new NativeHashMap<Entity, int>(totalCount, Allocator.TempJob);
        chunks = destroyedShipsQuery.CreateArchetypeChunkArray(Allocator.TempJob);
        foreach (ArchetypeChunk chunk in chunks)
        {
            ShipSpawnerOwnerSsShC owner = chunk.GetSharedComponentData(compType, em);
            Entity e = new Entity() { Index = owner.EntityIndex, Version = owner.EntityVer };
            int team = owner.EntityIndex;

            int count = chunk.Count;
            if (destroyedShipCounts.TryGetValue(e, out int val))
            {
                destroyedShipCounts.Remove(e);
                destroyedShipCounts.TryAdd(e, val + count);
            }
            else
            {
                destroyedShipCounts.TryAdd(e, count);
            }
        }

        // 2. Apply totals to components directly
        keys = destroyedShipCounts.GetKeyArray(Allocator.TempJob);
        vals = destroyedShipCounts.GetValueArray(Allocator.TempJob);
        for (int i = 0; i < keys.Length; i++)
        {
            Entity e = keys[i];
            if (em.Exists(e))
            {
                var spawner = em.GetComponentData<ShipSpawner>(e);
                spawner.ActiveShipCount -= vals[i];
                em.SetComponentData(e, spawner);
            }
        }

        return inputDeps;
    }
}