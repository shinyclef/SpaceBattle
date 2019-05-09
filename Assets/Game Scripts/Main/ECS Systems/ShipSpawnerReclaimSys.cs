using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

[UpdateInGroup(typeof(SpawnerGameGroup))]
public class ShipSpawnerReclaimSys : ComponentSystem
{
    private EntityQuery destroyedShipsQuery;

    protected override void OnCreate()
    {
        destroyedShipsQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(ShipSpawnerOwnerSsShC) },
            None = new ComponentType[] { typeof(SpawnTime) }
        });
    }

    protected override void OnUpdate()
    {
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
    }
}