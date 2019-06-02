using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(PhysicsGameGroup))]
[UpdateAfter(typeof(SpatialPartitionSys))]
public class NearestEnemySys : JobComponentSystem
{
    private const float MinUpdateInterval = 0.0f; // TODO: return nearest enemy search interval to 0.5f

    protected override void OnCreate()
    {
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var job = new GetNearestJob()
        {
            Time = Time.time,
            FactionComps = GetComponentDataFromEntity<Faction>(true),
            SpatialPartition = World.GetExistingSystem<SpatialPartitionSys>().SpatialPartition.ToConcurrent()
        };

        return inputDeps;
        //JobHandle jh = job.Schedule(this, inputDeps);
        //return jh;
    }

    [BurstCompile]
    private struct GetNearestJob : IJobNativeMultiHashMapVisitKeyValue<int2, Entity>
    {
        public float Time;
        [ReadOnly] public ComponentDataFromEntity<Faction> FactionComps;
        [ReadOnly] public NativeMultiHashMap<int2, Entity>.Concurrent SpatialPartition;

        public void ExecuteNext(int2 key, Entity value)
        {
            throw new System.NotImplementedException();
        }
    }

    //[BurstCompile]
    //private struct GetNearestJob : IJobForEachWithEntity<LocalToWorld, NearestEnemy>
    //{
    //    public float Time;
    //    [ReadOnly] public ComponentDataFromEntity<Faction> FactionComps;
    //    [ReadOnly] public NativeMultiHashMap<int2, Entity>.Concurrent SpatialPartition;

    //    public void Execute(Entity entity, int index, [ReadOnly] ref LocalToWorld l2w, ref NearestEnemy nearestEnemy)
    //    {
    //        if (Time - nearestEnemy.LastRefreshTime < MinUpdateInterval)
    //        {
    //            return;
    //        }

    //        Factions thisFaction = FactionComps[entity].Value;

    //        // search surrounding partitions that are in range
    //        float range = nearestEnemy.QueryRange;
    //        int2 zone = SpatialPartitionUtil.ToSpatialPartition(l2w.Position.xy);
    //        if (SpatialPartition.TryGetFirstValue(zone, out Entity entity, out NativeMultiHashMapIterator<int2> iterator))
    //        {
    //            do
    //            {
                    
    //            }
    //            while (SpatialPartition.TryGetNextValue(out entity, ref iterator));
    //        }




    //        Entity foundEntity = Entity.Null;
    //        if (foundEntity == entity || FactionComps[foundEntity].Value == thisFaction)
    //        {
    //            nearestEnemy.Entity = Entity.Null;
    //        }
    //        else
    //        {
    //            nearestEnemy.Entity = foundEntity;
    //        }

    //        nearestEnemy.LastRefreshTime = Time;
    //        //Logger.Log($"{entity} found {nearestEnemy.Entity}.");
    //    }
    //}
}