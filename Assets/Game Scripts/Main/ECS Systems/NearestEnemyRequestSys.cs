using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(MainGameGroup))]
[AlwaysUpdateSystem]
public class NearestEnemyRequestSys : JobComponentSystem
{
    private const float UpdateInterval = 1f;
    private float cycleTime;
    private float lastCycleTime;
    private int indexEnd;
    private int updateNumber;

    private EntityArchetype archetype;
    private NativeArray<Entity> bufferEntityPool;
    private NativeHashMap<int3, Entity> zoneTargetBuffers;
    private NativeHashMap<int3, int> requestedRegions;

    private NativeArray<int3> keys;
    private NativeArray<int> values;

    private BuildPhysicsWorld buildPhysicsWorldSys;
    private StepPhysicsWorld stepPhysicsWorldSys;
    private EntityQuery nearestEnemyReceiversQuery;

    public NativeHashMap<int3, Entity> ZoneTargetBuffers { get { return zoneTargetBuffers; } }

    protected override void OnCreate()
    {
        lastCycleTime = float.MaxValue;
        updateNumber = 0;
        archetype = EntityManager.CreateArchetype(typeof(NearbyEnemyBuf));

        bufferEntityPool = new NativeArray<Entity>(1, Allocator.Persistent);
        zoneTargetBuffers = new NativeHashMap<int3, Entity>(keys.Length, Allocator.Persistent);
        EnsureEnoughEntitiesInPool(1024);

        buildPhysicsWorldSys = World.GetOrCreateSystem<BuildPhysicsWorld>();
        stepPhysicsWorldSys = World.GetOrCreateSystem<StepPhysicsWorld>();

        nearestEnemyReceiversQuery = GetEntityQuery(typeof(LocalToWorld), typeof(NearestEnemy));
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (lastCycleTime > UpdateInterval && indexEnd == keys.Length)
        {
            //Logger.Log("NEW CYCLE!");
            lastCycleTime = 0f;
            indexEnd = 0;
            updateNumber++;
            inputDeps = PrepareNewCycle(inputDeps);
        }

        cycleTime = lastCycleTime + Time.deltaTime; // TODO: Confirm DeltaTime or FixedDeltaTime.
        if (indexEnd < keys.Length)
        {
            inputDeps = ContinueCycle(inputDeps);
        }

        lastCycleTime = cycleTime;
        //Logger.Log($"CycleTime: {cycleTime}");
        return inputDeps;
    }

    private JobHandle PrepareNewCycle(JobHandle inputDeps)
    {
        if (requestedRegions.IsCreated)
        {
            requestedRegions.Dispose();
        }

        int count = nearestEnemyReceiversQuery.CalculateLength();
        requestedRegions = new NativeHashMap<int3, int>(count, Allocator.Persistent);
        var populateRequestMapJob = new PopulateRequestMapJob()
        {
            UpdateNumber = updateNumber,
            RequestedRegions = requestedRegions.ToConcurrent()
        };

        inputDeps = populateRequestMapJob.Run(this, inputDeps);

        NativeArray<int3> zoneTargetBuffersKeys = zoneTargetBuffers.GetKeyArray(Allocator.TempJob);
        var removeEmptyZonesJob = new RemoveEmptyZonesJob
        {
            RequestedRegions = requestedRegions,
            ZoneTargetBuffers = zoneTargetBuffers,
            ZoneTargetBuffersKeys = zoneTargetBuffersKeys
        };

        inputDeps = removeEmptyZonesJob.Schedule(inputDeps);
        inputDeps.Complete();

        if (keys.IsCreated)
        {
            keys.Dispose();
            values.Dispose();
        }

        keys = requestedRegions.GetKeyArray(Allocator.Persistent);
        values = requestedRegions.GetValueArray(Allocator.Persistent);
        EnsureEnoughEntitiesInPool(keys.Length);
        
        return inputDeps;
    }

    private JobHandle ContinueCycle(JobHandle inputDeps)
    {
        int indexStart = indexEnd;
        indexEnd = UpdateInterval == 0f ? keys.Length : math.min(math.max((int)math.floor((cycleTime / UpdateInterval) * keys.Length), indexStart + 1), keys.Length);
        int indexLen = indexEnd - indexStart;
        //Logger.Log($"Ind: {indexStart}-{indexEnd} (keys: {keys.Length}.");

        stepPhysicsWorldSys.FinalJobHandle.Complete();
        var scanForEnemiesJob = new ScanForEnemiesJob()
        {
            IndexStart = indexStart,
            UpdateNumber = updateNumber,
            CollisionWorld = buildPhysicsWorldSys.PhysicsWorld.CollisionWorld,
            RequestedRegions = requestedRegions.ToConcurrent(),
            Keys = keys,
            Values = values,
            BufferEntityPool = bufferEntityPool,
            NearbyEnemyBufs = GetBufferFromEntity<NearbyEnemyBuf>(false),
            ZoneTargetBuffers = zoneTargetBuffers.ToConcurrent()
        };

        inputDeps = scanForEnemiesJob.Schedule(indexLen, 32, inputDeps);
        return inputDeps;
    }

    protected override void OnStopRunning()
    {
        Dispose();
    }

    private void Dispose()
    {
        if (requestedRegions.IsCreated)
        {
            requestedRegions.Dispose();
        }

        if (zoneTargetBuffers.IsCreated)
        {
            zoneTargetBuffers.Dispose();
        }
    }

    private void EnsureEnoughEntitiesInPool(int minCount)
    {
        if (bufferEntityPool.Length < minCount)
        {
            int newLen = bufferEntityPool.Length * 2;
            while (newLen < minCount)
            {
                newLen *= 2;
            }

            bufferEntityPool.Dispose();
            bufferEntityPool = new NativeArray<Entity>(newLen, Allocator.Persistent);
            EntityManager.CreateEntity(archetype, bufferEntityPool);

            var newArr = new NativeHashMap<int3, Entity>(newLen, Allocator.Persistent);
            NativeArray<int3> oldKeys = zoneTargetBuffers.GetKeyArray(Allocator.TempJob);
            for (int i = 0; i < oldKeys.Length; i++)
            {
                int3 key = oldKeys[i];
                newArr.TryAdd(key, zoneTargetBuffers[key]);
            }

            oldKeys.Dispose();
            zoneTargetBuffers.Dispose();
            zoneTargetBuffers = newArr;
        }
    }

    [BurstCompile]
    [RequireComponentTag(typeof(NearestEnemy))]
    private struct PopulateRequestMapJob : IJobForEach<LocalToWorld, Faction>
    {
        public int UpdateNumber;
        public NativeHashMap<int3, int>.Concurrent RequestedRegions;
        
        public void Execute([ReadOnly] ref LocalToWorld l2w, [ReadOnly] ref Faction faction)
        {
            int2 zone = SpatialPartitionUtil.ToSpatialPartition(l2w.Position.xy);
            int factionInt = (int)faction.Value;
            int3 bucket = new int3(zone.x, zone.y, factionInt);
            RequestedRegions.TryAdd(bucket, UpdateNumber);
        }
    }

    [BurstCompile]
    private struct RemoveEmptyZonesJob : IJob
    {
        [ReadOnly] public NativeHashMap<int3, int> RequestedRegions;
        public NativeHashMap<int3, Entity> ZoneTargetBuffers;
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<int3> ZoneTargetBuffersKeys;

        public void Execute()
        {
            // Iterate through ZoneTargetBuffersKeys. If it's found in RequestedRegions, remove it from ZoneTargetBuffers.
            for (int i = 0; i < ZoneTargetBuffersKeys.Length; i++)
            {
                int3 bucket = ZoneTargetBuffersKeys[i];
                if (!RequestedRegions.TryGetValue(bucket, out int val))
                {
                    ZoneTargetBuffers.Remove(bucket);
                }
            }
        }
    }

    [BurstCompile]
    private struct ScanForEnemiesJob : IJobParallelFor
    {
        public int IndexStart;
        public int UpdateNumber;
        [ReadOnly] public CollisionWorld CollisionWorld;
        [ReadOnly] public NativeHashMap<int3, int>.Concurrent RequestedRegions;
        [ReadOnly] public NativeArray<int3> Keys;
        [ReadOnly] public NativeArray<int> Values;
        [ReadOnly] public NativeArray<Entity> BufferEntityPool;
        [NativeDisableParallelForRestriction] public BufferFromEntity<NearbyEnemyBuf> NearbyEnemyBufs;
        public NativeHashMap<int3, Entity>.Concurrent ZoneTargetBuffers;

        public void Execute(int index)
        {
            int i = IndexStart + index;
            if (Values[i] < UpdateNumber)
            {
                return;
            }

            int3 bucket = Keys[i];
            Entity bufferEntity = BufferEntityPool[i];
            DynamicBuffer<NearbyEnemyBuf> buf = NearbyEnemyBufs[bufferEntity];
            buf.Clear();
            ZoneTargetBuffers.TryAdd(bucket, bufferEntity);

            unsafe
            {
                CollisionFilter filter = new CollisionFilter
                {
                    BelongsTo = 1u << (int)PhysicsLayer.RayCast,
                    CollidesWith = 1u << (int)PhysicsLayer.Ships,
                    GroupIndex = -bucket.z
                };

                PointDistanceInput pointInput = new PointDistanceInput
                {
                    Position = new float3(bucket.xy, 0f),
                    MaxDistance = 500f,
                    Filter = filter
                };

                DistanceHit hit;
                CollisionWorld.CalculateDistance(pointInput, out hit);
                if (CollisionWorld.Bodies[hit.RigidBodyIndex].Collider->Filter.GroupIndex != bucket.z) // TODO: Remove this temporary case when collider groups are working
                {
                    buf.Add(new NearbyEnemyBuf { Enemy = CollisionWorld.Bodies[hit.RigidBodyIndex].Entity });
                }

                //Logger.Log($"{entity} found {nearestEnemy.Entity}.");
            }
        }
    }
}
