using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(PhysicsGameGroup))]
public class SpatialPartitionSys : JobComponentSystem
{
    private NativeMultiHashMap<int2, Entity> spatialPartition;
    private EntityQuery query;

    public NativeMultiHashMap<int2, Entity> SpatialPartition { get { return spatialPartition; } }

    protected override void OnCreate()
    {
        query = GetEntityQuery(typeof(LocalToWorld), typeof(DoSpatialPartitionTag));
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (spatialPartition.IsCreated)
        {
            spatialPartition.Dispose();
        }

        int count = query.CalculateLength();
        spatialPartition = new NativeMultiHashMap<int2, Entity>(count, Allocator.TempJob);
        var populateSpatialMapJob = new PopulateSpatialMapJob()
        {
            SpatialPartition = spatialPartition.ToConcurrent()
        };

        JobHandle populateSpatialMapJH = populateSpatialMapJob.Schedule(this, inputDeps);
        populateSpatialMapJH.Complete();
        return populateSpatialMapJH;
    }

    protected override void OnStopRunning()
    {
        if (spatialPartition.IsCreated)
        {
            spatialPartition.Dispose();
        }
    }

    [BurstCompile]
    [RequireComponentTag(typeof(DoSpatialPartitionTag))]
    private struct PopulateSpatialMapJob : IJobForEachWithEntity<LocalToWorld>
    {
        public NativeMultiHashMap<int2, Entity>.Concurrent SpatialPartition;

        public void Execute(Entity entity, int index, [ReadOnly] ref LocalToWorld l2w)
        {
            SpatialPartition.Add(SpatialPartitionUtil.ToSpatialPartition(l2w.Position.xy), entity);
        }
    }
}