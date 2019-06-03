using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

[UpdateInGroup(typeof(MainGameGroup))]
[UpdateAfter(typeof(NearestEnemyRequestSys))]
public class NearestEnemySys : JobComponentSystem
{
    private NearestEnemyRequestSys nearestEnemyRequestSys;
    private const float MinUpdateInterval = 0.0f; // TODO: return nearest enemy search interval to 0.5f

    protected override void OnCreate()
    {
        nearestEnemyRequestSys = World.GetOrCreateSystem<NearestEnemyRequestSys>();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (!nearestEnemyRequestSys.ZoneTargetBuffers.IsCreated)
        {
            return inputDeps;
        }

        var getBufferEntityJob = new GetBufferEntityJob
        {
            ZoneTargetBuffers = nearestEnemyRequestSys.ZoneTargetBuffers
        };

        inputDeps = getBufferEntityJob.Schedule(this, inputDeps);

        var assignTargetJob = new AssignTargetJob
        {
            NearbyEnemyBufs = GetBufferFromEntity<NearbyEnemyBuf>(true)
        };

        inputDeps = assignTargetJob.Schedule(this, inputDeps);
        return inputDeps;
    }

    [BurstCompile]
    private struct GetBufferEntityJob : IJobForEach<LocalToWorld, Faction, NearestEnemy>
    {
        [ReadOnly] public NativeHashMap<int3, Entity> ZoneTargetBuffers;

        public void Execute([ReadOnly] ref LocalToWorld l2w, [ReadOnly] ref Faction faction, ref NearestEnemy enemy)
        {
            int2 zone = SpatialPartitionUtil.ToSpatialPartition(l2w.Position.xy);
            int factionInt = (int)faction.Value;
            int3 bucket = new int3(zone.x, zone.y, factionInt);

            Entity e;
            ZoneTargetBuffers.TryGetValue(bucket, out e);
            enemy.ZoneTargetBufferEntity = e;
        }
    }

    [BurstCompile]
    private struct AssignTargetJob : IJobForEach<NearestEnemy>
    {
        [ReadOnly] public BufferFromEntity<NearbyEnemyBuf> NearbyEnemyBufs;

        public void Execute(ref NearestEnemy enemy)
        {
            Entity bufEntity = enemy.ZoneTargetBufferEntity;
            if (NearbyEnemyBufs.Exists(bufEntity))
            {
                DynamicBuffer<NearbyEnemyBuf> buf = NearbyEnemyBufs[bufEntity];
                enemy.Entity = buf[0];
            }
            else
            {
                enemy.Entity = Entity.Null;
            }
        }
    }
}