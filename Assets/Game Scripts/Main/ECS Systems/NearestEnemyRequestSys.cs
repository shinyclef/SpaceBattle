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
    public const float UpdateInterval = 1f;

    private EntityArchetype archetype;
    private NativeQueue<Entity> bufferEntityPool;
    private NativeHashMap<int3, Entity> nearestEnemiesBuffers;
    private NativeHashMap<int3, float> activeZones;
    private NativeHashMap<int3, byte> queriedZones;
    private NativeList<int3> keys;

    private BuildPhysicsWorld buildPhysicsWorldSys;
    private StepPhysicsWorld stepPhysicsWorldSys;
    private EntityQuery nearestEnemyReceiversQuery;

    public JobHandle FinalJobHandle { get; private set; }
    public NativeHashMap<int3, Entity> NearestEnemiesBuffers { get { return nearestEnemiesBuffers; } }

    protected override void OnCreate()
    {
        archetype = EntityManager.CreateArchetype(typeof(NearbyEnemyBuf));

        bufferEntityPool = new NativeQueue<Entity>(Allocator.Persistent);
        bufferEntityPool.Enqueue(EntityManager.CreateEntity(archetype));
        nearestEnemiesBuffers = new NativeHashMap<int3, Entity>(1, Allocator.Persistent);
        activeZones = new NativeHashMap<int3, float>(1, Allocator.Persistent);
        keys = new NativeList<int3>(Allocator.Persistent);
        EnsureMinimumCapacity(1024);

        buildPhysicsWorldSys = World.GetOrCreateSystem<BuildPhysicsWorld>();
        stepPhysicsWorldSys = World.GetOrCreateSystem<StepPhysicsWorld>();
        nearestEnemyReceiversQuery = GetEntityQuery(typeof(LocalToWorld), typeof(NearestEnemy));
    }

    private void PrepareQueriedZonesMap(int capacity)
    {
        if (queriedZones.IsCreated)
        {
            queriedZones.Dispose();
        }

        queriedZones = new NativeHashMap<int3, byte>(capacity, Allocator.TempJob);
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        int count = nearestEnemyReceiversQuery.CalculateLength();
        EnsureMinimumCapacity(count);
        PrepareQueriedZonesMap(count);

        // 1. Populate queriedZones, parallel
        inputDeps = new PopulateQueriedZonesJob()
        {
            QueriedZones = queriedZones.ToConcurrent(),
        }.Schedule(this, inputDeps);

        // 2. Update activeZones, single thread
        inputDeps = new UpdateActiveZonesMapJob()
        {
            ActiveZones = activeZones,
            QueriedZones = queriedZones,
            NearestEnemiesBuffers = nearestEnemiesBuffers,
            BufferEntityPool = bufferEntityPool,
            NearbyEnemyBufs = GetBufferFromEntity<NearbyEnemyBuf>(false),
            Keys = keys,
            L2Ws = GetComponentDataFromEntity<LocalToWorld>(false)
        }.Schedule(inputDeps);

        // 3. Refresh the zones
        inputDeps = JobHandle.CombineDependencies(inputDeps, stepPhysicsWorldSys.FinalJobHandle);
        inputDeps = new ScanForEnemiesJob()
        {
            Time = Time.time,
            CollisionWorld = buildPhysicsWorldSys.PhysicsWorld.CollisionWorld,
            ActiveZones = activeZones,
            ZoneKeys = keys.AsDeferredJobArray(),
            NearbyEnemyBufs = GetBufferFromEntity<NearbyEnemyBuf>(false),
            NearestEnemiesBuffers = nearestEnemiesBuffers
        }.Schedule(keys, 4, inputDeps);

        FinalJobHandle = inputDeps;
        return inputDeps;
    }

    protected override void OnDestroy()
    {
        DisposeAll();
    }

    private void DisposeAll()
    {
        if (queriedZones.IsCreated)
        {
            queriedZones.Dispose();
        }

        if (activeZones.IsCreated)
        {
            activeZones.Dispose();
        }

        if (bufferEntityPool.IsCreated)
        {
            bufferEntityPool.Dispose();
        }

        if (nearestEnemiesBuffers.IsCreated)
        {
            nearestEnemiesBuffers.Dispose();
        }

        if (keys.IsCreated)
        {
            keys.Dispose();
        }
    }

    private void EnsureMinimumCapacity(int minCount)
    {
        if (activeZones.Capacity < minCount)
        {
            int newLen = math.max(activeZones.Length * 2, 1);
            while (newLen < minCount)
            {
                newLen *= 2;
            }

            var newPoolEntities = new NativeArray<Entity>(newLen - activeZones.Length, Allocator.TempJob);
            EntityManager.CreateEntity(archetype, newPoolEntities);
            for (int i = 0; i < newPoolEntities.Length; i++)
            {
                bufferEntityPool.Enqueue(newPoolEntities[i]);
            }

            newPoolEntities.Dispose();

            var newZoneTargetBuffers = new NativeHashMap<int3, Entity>(newLen, Allocator.Persistent);
            NativeArray<int3> oldKeys = nearestEnemiesBuffers.GetKeyArray(Allocator.TempJob);
            for (int i = 0; i < oldKeys.Length; i++)
            {
                int3 key = oldKeys[i];
                newZoneTargetBuffers.TryAdd(key, nearestEnemiesBuffers[key]);
            }

            oldKeys.Dispose();
            nearestEnemiesBuffers.Dispose();
            nearestEnemiesBuffers = newZoneTargetBuffers;

            var newActiveZones = new NativeHashMap<int3, float>(newLen, Allocator.Persistent);
            oldKeys = activeZones.GetKeyArray(Allocator.TempJob);
            for (int i = 0; i < oldKeys.Length; i++)
            {
                int3 key = oldKeys[i];
                newActiveZones.TryAdd(key, activeZones[key]);
            }

            for (int i = 0; i < oldKeys.Length; i++)
            {
                int3 key = oldKeys[i];
                newActiveZones.TryAdd(key, activeZones[key]);
            }

            oldKeys.Dispose();
            activeZones.Dispose();
            activeZones = newActiveZones;
        }
    }

    [BurstCompile]
    private struct PopulateQueriedZonesJob : IJobForEach<LocalToWorld, Faction, NearestEnemy>
    {
        public NativeHashMap<int3, byte>.Concurrent QueriedZones;

        public void Execute([ReadOnly] ref LocalToWorld l2w, [ReadOnly] ref Faction faction, [ReadOnly] ref NearestEnemy nearestEnemy)
        {
            if (nearestEnemy.UpdatePending)
            {
                int2 zone = SpatialPartitionUtil.ToSpatialPartition(l2w.Position.xy);
                int factionInt = (int)faction.Value;
                int3 bucket = new int3(zone.x, zone.y, factionInt);
                QueriedZones.TryAdd(bucket, 0);
            }
        }
    }

    [BurstCompile]
    private struct UpdateActiveZonesMapJob : IJob
    {
        public NativeHashMap<int3, float> ActiveZones;
        [ReadOnly] public NativeHashMap<int3, byte> QueriedZones;
        public NativeHashMap<int3, Entity> NearestEnemiesBuffers;
        public NativeQueue<Entity> BufferEntityPool;
        public BufferFromEntity<NearbyEnemyBuf> NearbyEnemyBufs;
        public NativeList<int3> Keys;
        public ComponentDataFromEntity<LocalToWorld> L2Ws;

        public void Execute()
        {
            // 1. Remove zone if not in QueriedZones, set last update time to float.min if the zone has no existing targets left
            var activeZoneKeys = ActiveZones.GetKeyArray(Allocator.Temp);
            for (int i = 0; i < activeZoneKeys.Length; i++)
            {
                int3 bucket = activeZoneKeys[i];
                if (!QueriedZones.TryGetValue(bucket, out _))
                {
                    // clear and remove the nearest enemies buffer
                    DynamicBuffer<NearbyEnemyBuf> buf = NearbyEnemyBufs[NearestEnemiesBuffers[bucket]];
                    buf.Clear();
                    NearestEnemiesBuffers.TryGetValue(bucket, out Entity e);
                    {
                        BufferEntityPool.Enqueue(e);
                    }

                    NearestEnemiesBuffers.Remove(bucket);

                    // remove from AcitiveZones
                    ActiveZones.Remove(bucket);
                }
                else
                {
                    DynamicBuffer<NearbyEnemyBuf> buf = NearbyEnemyBufs[NearestEnemiesBuffers[bucket]];
                    for (int j = buf.Length - 1; j >= 0; j--)
                    {
                        if (!L2Ws.Exists(buf[j]))
                        {
                            buf.RemoveAt(j);
                        }
                    }

                    if (buf.Length == 0)
                    {
                        ActiveZones.Remove(bucket);
                    }
                    else
                    {
                        ActiveZones.Remove(bucket);
                        ActiveZones.TryAdd(bucket, float.MinValue);
                    }
                }
            }

            // 2. Add new zones from QueriedZones
            var queriedZoneKeys = QueriedZones.GetKeyArray(Allocator.Temp);
            for (int i = 0; i < queriedZoneKeys.Length; i++)
            {
                int3 bucket = queriedZoneKeys[i];
                if (!ActiveZones.TryGetValue(bucket, out _))
                {
                    // add nearest enemy buffer
                    NearestEnemiesBuffers.TryAdd(bucket, BufferEntityPool.Dequeue());

                    // add to ActiveZones
                    ActiveZones.TryAdd(bucket, float.MinValue);
                }
            }

            var tempKeys = ActiveZones.GetKeyArray(Allocator.Temp);
            Keys.Clear();
            Keys.AddRange(tempKeys);
        }
    }

    [BurstCompile]
    private struct ScanForEnemiesJob : IJobParallelForDefer
    {
        private const float ScanRange = 600f;

        public float Time;
        [ReadOnly] public CollisionWorld CollisionWorld;
        [ReadOnly] public NativeHashMap<int3, float> ActiveZones;
        [ReadOnly] public NativeArray<int3> ZoneKeys;
        [NativeDisableParallelForRestriction] public BufferFromEntity<NearbyEnemyBuf> NearbyEnemyBufs;
        [NativeDisableParallelForRestriction] public NativeHashMap<int3, Entity> NearestEnemiesBuffers;

        public void Execute(int index)
        {
            int3 bucket = ZoneKeys[index];
            ActiveZones.TryGetValue(bucket, out float lastUpdateTime);
            if (Time - lastUpdateTime < UpdateInterval)
            {
                return;
            }

            NearestEnemiesBuffers.TryGetValue(bucket, out Entity bufferEntity);
            DynamicBuffer<NearbyEnemyBuf> buf = NearbyEnemyBufs[bufferEntity];
            buf.Clear();

            unsafe
            {
                PointDistanceInput pointInput = new PointDistanceInput
                {
                    Position = new float3(bucket.xy, 0f),
                    MaxDistance = ScanRange,
                    Filter = new CollisionFilter
                    {
                        BelongsTo = 1u << (int)PhysicsLayer.RayCast,
                        CollidesWith = 1u << (int)PhysicsLayer.Ships,
                        GroupIndex = -bucket.z
                    }
                };

                DistanceHit hit;
                if (CollisionWorld.CalculateDistance(pointInput, out hit))
                {
                    buf.Add(CollisionWorld.Bodies[hit.RigidBodyIndex].Entity);
                }
            }
        }
    }
}
