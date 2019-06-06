using System;
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
//[AlwaysUpdateSystem]
[DisableAutoCreation]
public class NearestEnemyRequestSysAttempt1 : JobComponentSystem
{
    public const float UpdateInterval = 0.0f;
    private float cycleTime;
    private float lastCycleTime;
    private int cycleIndexEnd;
    private int originalZonesLength;
    private int incrementalZonesLength;

    private EntityArchetype archetype;
    private NativeArray<Entity> bufferEntityPool;
    private NativeHashMap<int3, Entity> zoneTargetBuffers;
    private NativeHashMap<int3, int> requestedZones;
    private NativeArray<int3> requestedZoneKeys;

    private BuildPhysicsWorld buildPhysicsWorldSys;
    private StepPhysicsWorld stepPhysicsWorldSys;
    private EntityQuery nearestEnemyReceiversQuery;

    public NativeHashMap<int3, Entity> ZoneTargetBuffers { get { return zoneTargetBuffers; } }

    protected override void OnCreate()
    {
        lastCycleTime = float.MaxValue;
        archetype = EntityManager.CreateArchetype(typeof(NearbyEnemyBuf));

        bufferEntityPool = new NativeArray<Entity>(1, Allocator.Persistent);
        zoneTargetBuffers = new NativeHashMap<int3, Entity>(1, Allocator.Persistent);
        requestedZones = new NativeHashMap<int3, int>(1, Allocator.Persistent);
        EnsureMinimumCapacity(1024);

        buildPhysicsWorldSys = World.GetOrCreateSystem<BuildPhysicsWorld>();
        stepPhysicsWorldSys = World.GetOrCreateSystem<StepPhysicsWorld>();

        nearestEnemyReceiversQuery = GetEntityQuery(typeof(LocalToWorld), typeof(NearestEnemy));
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (lastCycleTime >= UpdateInterval && cycleIndexEnd == originalZonesLength)
        {
            //Logger.Log("NEW CYCLE!");
            lastCycleTime = 0f;
            cycleIndexEnd = 0;
            inputDeps = PrepareNewCycle(inputDeps);
        }
        else
        {
            inputDeps = HandleIncrementalAdditions(inputDeps);
        }

        cycleTime = lastCycleTime + Time.deltaTime; // TODO: Confirm DeltaTime or FixedDeltaTime.
        if (cycleIndexEnd < originalZonesLength)
        {
            inputDeps = ContinueCycle(inputDeps);
        }

        lastCycleTime = cycleTime;
        //Logger.Log($"CycleTime: {cycleTime}");
        return inputDeps;
    }

    private JobHandle PrepareNewCycle(JobHandle inputDeps)
    {
        requestedZones.Clear();
        int count = nearestEnemyReceiversQuery.CalculateLength();
        EnsureMinimumCapacity(count);

        var populateRequestMapJob = new PopulateRequestMapJob()
        {
            RequestedZones = requestedZones.ToConcurrent()
        };

        inputDeps = populateRequestMapJob.Run(this, inputDeps);

        NativeArray<int3> zoneTargetBuffersKeys = zoneTargetBuffers.GetKeyArray(Allocator.TempJob);
        var removeEmptyZonesJob = new RemoveEmptyZonesJob
        {
            RequestedZones = requestedZones,
            ZoneTargetBuffers = zoneTargetBuffers,
            ZoneTargetBuffersKeys = zoneTargetBuffersKeys,
            NearbyEnemyBufs = GetBufferFromEntity<NearbyEnemyBuf>(false),
        };

        inputDeps = removeEmptyZonesJob.Schedule(inputDeps);
        inputDeps.Complete();

        if (requestedZoneKeys.IsCreated)
        {
            requestedZoneKeys.Dispose();
        }

        requestedZoneKeys = requestedZones.GetKeyArray(Allocator.TempJob);
        originalZonesLength = requestedZoneKeys.Length;
        incrementalZonesLength = originalZonesLength;
        
        return inputDeps;
    }

    private JobHandle HandleIncrementalAdditions(JobHandle inputDeps)
    {
        // any zones that no longer have an active target (perhaps it was destroyed) need to be removed
        var zoneTargetBuffersKeys = ZoneTargetBuffers.GetKeyArray(Allocator.TempJob);
        var clearZonesWithNoActiveTargetJob = new ClearZonesWithNoActiveTargetJob
        {
            ZoneTargetBuffers = zoneTargetBuffers,
            ZoneTargetBuffersKeys = zoneTargetBuffersKeys,
            NearbyEnemyBufs = GetBufferFromEntity<NearbyEnemyBuf>(true),
            LocalToWorlds = GetComponentDataFromEntity<LocalToWorld>(true),
        };

        inputDeps = clearZonesWithNoActiveTargetJob.Schedule(inputDeps);
        inputDeps.Complete();

        // for any zones entered just now, we need to produce a scan immediately so nearest enemy searches do return something
        int count = nearestEnemyReceiversQuery.CalculateLength();
        EnsureMinimumCapacity(count);

        var populateRequestMapJob = new PopulateRequestMapJob()
        {
            RequestedZones = requestedZones.ToConcurrent()
        };

        inputDeps = populateRequestMapJob.Schedule(this, inputDeps);
        inputDeps.Complete();

        if (requestedZoneKeys.IsCreated)
        {
            requestedZoneKeys.Dispose();
        }

        requestedZoneKeys = requestedZones.GetKeyArray(Allocator.TempJob);

        // scan for all inremental zones now. we're at the end of the requestedZoneKeys array started from what we added this frame
        var scanForEnemiesJob = new ScanForEnemiesJob()
        {
            IndexStart = incrementalZonesLength,
            CollisionWorld = buildPhysicsWorldSys.PhysicsWorld.CollisionWorld,
            RequestedZones = requestedZones.ToConcurrent(),
            RequestedZonekeys = requestedZoneKeys,
            BufferEntityPool = bufferEntityPool,
            NearbyEnemyBufs = GetBufferFromEntity<NearbyEnemyBuf>(false),
            ZoneTargetBuffers = zoneTargetBuffers.ToConcurrent()
        };

        //Logger.LogIf(requestedZoneKeys.Length - incrementalZonesLength > 0, $"Incremental: {requestedZoneKeys.Length - incrementalZonesLength}");
        inputDeps = scanForEnemiesJob.Schedule(requestedZoneKeys.Length - incrementalZonesLength, 4, inputDeps);
        incrementalZonesLength = requestedZoneKeys.Length;

        return inputDeps;
    }

    private JobHandle ContinueCycle(JobHandle inputDeps)
    {
        int indexStart = cycleIndexEnd;
        cycleIndexEnd = UpdateInterval == 0f ? originalZonesLength : math.min(math.max((int)math.floor((cycleTime / UpdateInterval) * originalZonesLength), indexStart + 1), originalZonesLength);
        int indexLen = cycleIndexEnd - indexStart;
        //Logger.Log($"Ind: {indexStart}-{indexEnd} (keys: {keys.Length}.");

        var clearZonesAboutToBeScannedJob = new ClearZonesAboutToBeScannedJob
        {
            IndexStart = indexStart,
            IndexEnd = cycleIndexEnd,
            RequestedZoneKeys = requestedZoneKeys,
            ZoneTargetBuffers = zoneTargetBuffers
        };

        inputDeps = clearZonesAboutToBeScannedJob.Schedule(inputDeps);
        inputDeps.Complete();
        stepPhysicsWorldSys.FinalJobHandle.Complete();
        var scanForEnemiesJob = new ScanForEnemiesJob()
        {
            IndexStart = indexStart,
            CollisionWorld = buildPhysicsWorldSys.PhysicsWorld.CollisionWorld,
            RequestedZones = requestedZones.ToConcurrent(),
            RequestedZonekeys = requestedZoneKeys,
            BufferEntityPool = bufferEntityPool,
            NearbyEnemyBufs = GetBufferFromEntity<NearbyEnemyBuf>(false),
            ZoneTargetBuffers = zoneTargetBuffers.ToConcurrent()
        };

        inputDeps = scanForEnemiesJob.Schedule(indexLen, 4, inputDeps);
        return inputDeps;
    }

    protected override void OnDestroy()
    {
        DisposeAll();
    }

    private void DisposeAll()
    {
        if (requestedZones.IsCreated)
        {
            requestedZones.Dispose();
        }

        if (requestedZoneKeys.IsCreated)
        {
            requestedZoneKeys.Dispose();
        }

        if (zoneTargetBuffers.IsCreated)
        {
            zoneTargetBuffers.Dispose();
        }

        if (bufferEntityPool.IsCreated)
        {
            bufferEntityPool.Dispose();
        }
    }

    private void EnsureMinimumCapacity(int minCount)
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

            var newZoneTargetBuffers = new NativeHashMap<int3, Entity>(newLen, Allocator.Persistent);
            NativeArray<int3> oldKeys = zoneTargetBuffers.GetKeyArray(Allocator.TempJob);
            for (int i = 0; i < oldKeys.Length; i++)
            {
                int3 key = oldKeys[i];
                newZoneTargetBuffers.TryAdd(key, zoneTargetBuffers[key]);
            }

            oldKeys.Dispose();
            zoneTargetBuffers.Dispose();
            zoneTargetBuffers = newZoneTargetBuffers;

            var newRequestedZones = new NativeHashMap<int3, int>(newLen, Allocator.Persistent);
            oldKeys = requestedZones.GetKeyArray(Allocator.TempJob);
            for (int i = 0; i < oldKeys.Length; i++)
            {
                int3 key = oldKeys[i];
                newRequestedZones.TryAdd(key, requestedZones[key]);
            }

            oldKeys.Dispose();
            requestedZones.Dispose();
            requestedZones = newRequestedZones;
        }
    }

    #region Prepare New Cycle Jobs

    [BurstCompile]
    private struct PopulateRequestMapJob : IJobForEach<LocalToWorld, Faction, NearestEnemy>
    {
        public NativeHashMap<int3, int>.Concurrent RequestedZones;
        
        public void Execute([ReadOnly] ref LocalToWorld l2w, [ReadOnly] ref Faction faction, [ReadOnly] ref NearestEnemy nearestEnemy)
        {
            int2 zone = SpatialPartitionUtil.ToSpatialPartition(l2w.Position.xy);
            int factionInt = (int)faction.Value;
            int3 bucket = new int3(zone.x, zone.y, factionInt);
            RequestedZones.TryAdd(bucket, 0);
        }
    }

    [BurstCompile]
    private struct RemoveEmptyZonesJob : IJob
    {
        [ReadOnly] public NativeHashMap<int3, int> RequestedZones;
        public NativeHashMap<int3, Entity> ZoneTargetBuffers;
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<int3> ZoneTargetBuffersKeys;
        public BufferFromEntity<NearbyEnemyBuf> NearbyEnemyBufs;

        public void Execute()
        {
            // Iterate through ZoneTargetBuffersKeys. If it's not found in RequestedZones, remove it from ZoneTargetBuffers.
            for (int i = 0; i < ZoneTargetBuffersKeys.Length; i++)
            {
                int3 bucket = ZoneTargetBuffersKeys[i];
                NearbyEnemyBufs[ZoneTargetBuffers[bucket]].Clear();
                if (!RequestedZones.TryGetValue(bucket, out int val))
                {
                    ZoneTargetBuffers.Remove(bucket);
                }
            }
        }
    }

    #endregion

    #region Continuous Cycle Jobs

    [BurstCompile]
    private struct ClearZonesWithNoActiveTargetJob : IJob
    {
        public NativeHashMap<int3, Entity> ZoneTargetBuffers;
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<int3> ZoneTargetBuffersKeys;
        [ReadOnly] public BufferFromEntity<NearbyEnemyBuf> NearbyEnemyBufs;
        [ReadOnly] public ComponentDataFromEntity<LocalToWorld> LocalToWorlds;

        public void Execute()
        {
            for (int i = 0; i < ZoneTargetBuffersKeys.Length; i++)
            {
                int3 bucket = ZoneTargetBuffersKeys[i];
                if (ZoneTargetBuffers.TryGetValue(bucket, out Entity bufEntity))
                {
                    DynamicBuffer<NearbyEnemyBuf> buf = NearbyEnemyBufs[bufEntity];
                    bool found = false;
                    for (int j = 0; j < buf.Length; j++)
                    {
                        found = true;
                        break;
                    }

                    if (!found)
                    {
                        ZoneTargetBuffers.Remove(bucket);
                    }
                }
            }
        }
    }

    [BurstCompile]
    private struct PopulateIncrementalRequestedZonesJob : IJobForEach<LocalToWorld, Faction, NearestEnemy>
    {
        [NativeDisableParallelForRestriction] [ReadOnly] public NativeHashMap<int3, int> RequestedZones;
        public NativeHashMap<int3, int>.Concurrent NewZones;

        public void Execute([ReadOnly] ref LocalToWorld l2w, [ReadOnly] ref Faction faction, [ReadOnly] ref NearestEnemy nearestEnemy)
        {
            int2 zone = SpatialPartitionUtil.ToSpatialPartition(l2w.Position.xy);
            int factionInt = (int)faction.Value;
            int3 bucket = new int3(zone.x, zone.y, factionInt);

            if (!RequestedZones.TryGetValue(bucket, out int val))
            {
                NewZones.TryAdd(bucket, 0);
            }
        }
    }

    [BurstCompile]
    private struct ClearZonesAboutToBeScannedJob : IJob
    {
        public int IndexStart;
        public int IndexEnd;
        [ReadOnly] public NativeArray<int3> RequestedZoneKeys;
        public NativeHashMap<int3, Entity> ZoneTargetBuffers;

        public void Execute()
        {
            for (int i = IndexStart; i < IndexEnd; i++)
            {
                int3 bucket = RequestedZoneKeys[i];
                if (ZoneTargetBuffers.TryGetValue(bucket, out Entity val))
                {
                    ZoneTargetBuffers.Remove(bucket);
                }
            }
        }
    }

    [BurstCompile]
    private struct ScanForEnemiesJob : IJobParallelFor
    {
        private const float ScanRange = 600f;

        public int IndexStart;
        [ReadOnly] public CollisionWorld CollisionWorld;
        [ReadOnly] public NativeHashMap<int3, int>.Concurrent RequestedZones;
        [ReadOnly] public NativeArray<int3> RequestedZonekeys;
        [ReadOnly] public NativeArray<Entity> BufferEntityPool;
        [NativeDisableParallelForRestriction] public BufferFromEntity<NearbyEnemyBuf> NearbyEnemyBufs;
        public NativeHashMap<int3, Entity>.Concurrent ZoneTargetBuffers;

        public void Execute(int index)
        {
            int3 bucket = RequestedZonekeys[IndexStart + index];
            Entity bufferEntity = BufferEntityPool[IndexStart + index];
            if (!ZoneTargetBuffers.TryAdd(bucket, bufferEntity))
            {
                return;
            }

            DynamicBuffer<NearbyEnemyBuf> buf = NearbyEnemyBufs[bufferEntity];
            buf.Clear();

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
                    MaxDistance = ScanRange,
                    Filter = filter
                };

                DistanceHit hit;
                if (CollisionWorld.CalculateDistance(pointInput, out hit))
                {
                    // TODO: Remove this temporary case when collider groups are working
                    if (CollisionWorld.Bodies[hit.RigidBodyIndex].Collider->Filter.GroupIndex == -bucket.z) 
                    {
                        throw new Exception("Found my own team... that's not good");
                    }

                    buf.Add(CollisionWorld.Bodies[hit.RigidBodyIndex].Entity);
                }
            }
        }
    }

    #endregion
}
