﻿using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Assertions;

[UpdateInGroup(typeof(MainGameGroup))]
[AlwaysUpdateSystem]
public class NearestEnemyRequestSys : JobComponentSystem
{
    public const float UpdateInterval = 1.5f;

    private EntityArchetype archetype;
    private NativeQueue<Entity> bufferEntityPool;
    private NativeHashMap<int3, Entity> nearestEnemiesBufferEntitiesMap;
    private NativeHashMap<int3, float> activeZones;
    private NativeHashMap<int3, byte> queriedZones;
    private NativeList<int3> keys;

    private BuildPhysicsWorld buildPhysicsWorldSys;
    private EndFramePhysicsSystem endFramePhysicsSys;
    private EntityQuery nearestEnemyReceiversQuery;

    public JobHandle FinalJobHandle { get; private set; }
    public NativeHashMap<int3, Entity> NearestEnemiesBuffers { get { return nearestEnemiesBufferEntitiesMap; } }

    protected override void OnCreate()
    {
        archetype = EntityManager.CreateArchetype(typeof(NearbyEnemyBuf));

        bufferEntityPool = new NativeQueue<Entity>(Allocator.Persistent);
        bufferEntityPool.Enqueue(EntityManager.CreateEntity(archetype));
        nearestEnemiesBufferEntitiesMap = new NativeHashMap<int3, Entity>(1, Allocator.Persistent);
        activeZones = new NativeHashMap<int3, float>(1, Allocator.Persistent);
        keys = new NativeList<int3>(Allocator.Persistent);
        EnsureMinimumCapacity(1024);

        buildPhysicsWorldSys = World.GetOrCreateSystem<BuildPhysicsWorld>();
        endFramePhysicsSys = World.GetOrCreateSystem<EndFramePhysicsSystem>();
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
        int queriedZonesCount = nearestEnemyReceiversQuery.CalculateEntityCount();
        EnsureMinimumCapacity(queriedZonesCount);
        PrepareQueriedZonesMap(queriedZonesCount);

        // 1. Populate queriedZones, parallel
        inputDeps = new PopulateQueriedZonesJob()
        {
            QueriedZones = queriedZones.AsParallelWriter(),
        }.Schedule(this, inputDeps);

        // 2. Update activeZones, single thread
        inputDeps = new UpdateActiveZonesMapJob()
        {
            Time = Time.time,
            ActiveZones = activeZones,
            QueriedZones = queriedZones,
            NearestEnemiesBufferEntitiesMap = nearestEnemiesBufferEntitiesMap,
            BufferEntityPool = bufferEntityPool,
            NearbyEnemyBufs = GetBufferFromEntity<NearbyEnemyBuf>(false),
            Keys = keys,
            L2Ws = GetComponentDataFromEntity<LocalToWorld>(false)
        }.Schedule(inputDeps);

        // 3. Refresh the zones
        inputDeps = JobHandle.CombineDependencies(inputDeps, endFramePhysicsSys.FinalJobHandle);
        inputDeps = new ScanForEnemiesJob()
        {
            Time = Time.time,
            CollisionWorld = buildPhysicsWorldSys.PhysicsWorld.CollisionWorld,
            ActiveZones = activeZones,
            ZoneKeys = keys.AsDeferredJobArray(),
            NearbyEnemyBufs = GetBufferFromEntity<NearbyEnemyBuf>(false),
            NearestEnemiesBufferEntitiesMap = nearestEnemiesBufferEntitiesMap
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

        if (nearestEnemiesBufferEntitiesMap.IsCreated)
        {
            nearestEnemiesBufferEntitiesMap.Dispose();
        }

        if (keys.IsCreated)
        {
            keys.Dispose();
        }
    }

    private void EnsureMinimumCapacity(int minCount)
    {
        // Guarantee there are enough nearby enemy buffer entitites
        int newBufferEntityCount = minCount - bufferEntityPool.Count;
        if (newBufferEntityCount > 0)
        {
            var newPoolEntities = new NativeArray<Entity>(newBufferEntityCount, Allocator.TempJob);
            EntityManager.CreateEntity(archetype, newPoolEntities);
            for (int i = 0; i < newPoolEntities.Length; i++)
            {
                bufferEntityPool.Enqueue(newPoolEntities[i]);
            }

            newPoolEntities.Dispose();
        }
        
        // increase capacity of other collections
        if (activeZones.Capacity < minCount)
        {
            int newLen = math.max(activeZones.Length * 2, 1);
            while (newLen < minCount)
            {
                newLen *= 2;
            }

            var newZoneTargetBuffers = new NativeHashMap<int3, Entity>(newLen, Allocator.Persistent);
            NativeArray<int3> oldKeys = nearestEnemiesBufferEntitiesMap.GetKeyArray(Allocator.TempJob);
            for (int i = 0; i < oldKeys.Length; i++)
            {
                int3 key = oldKeys[i];
                newZoneTargetBuffers.TryAdd(key, nearestEnemiesBufferEntitiesMap[key]);
            }

            oldKeys.Dispose();
            nearestEnemiesBufferEntitiesMap.Dispose();
            nearestEnemiesBufferEntitiesMap = newZoneTargetBuffers;

            var newActiveZones = new NativeHashMap<int3, float>(newLen, Allocator.Persistent);
            oldKeys = activeZones.GetKeyArray(Allocator.TempJob);
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
        public NativeHashMap<int3, byte>.ParallelWriter QueriedZones;

        public void Execute([ReadOnly] ref LocalToWorld l2w, [ReadOnly] ref Faction faction, [ReadOnly] ref NearestEnemy nearestEnemy)
        {
            if (nearestEnemy.UpdateRequired)
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
        public float Time;
        public NativeHashMap<int3, float> ActiveZones;
        [ReadOnly] public NativeHashMap<int3, byte> QueriedZones;
        public NativeHashMap<int3, Entity> NearestEnemiesBufferEntitiesMap;
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
                ActiveZones.TryGetValue(bucket, out float lastUpdateTime);
                if (Time - lastUpdateTime >= UpdateInterval && !QueriedZones.TryGetValue(bucket, out _))
                {
                    // clear and remove the nearest enemies buffer
                    DynamicBuffer<NearbyEnemyBuf> buf = NearbyEnemyBufs[NearestEnemiesBufferEntitiesMap[bucket]];
                    buf.Clear();
                    NearestEnemiesBufferEntitiesMap.TryGetValue(bucket, out Entity e);
                    {
                        BufferEntityPool.Enqueue(e);
                        //Logger.Log($"{FRAME}: Enqueue A. Len {BufferEntityPool.Count}");
                    }

                    NearestEnemiesBufferEntitiesMap.Remove(bucket);
                    ActiveZones.Remove(bucket);
                    //Logger.Log($"{FRAME}: Remove {bucket} !QueriedZones.Contains. Last Update: {lastUpdateTime}");
                }
                else // the zone is being queried this frame (we found it in QueriedZones or it's cached)
                {
                    DynamicBuffer<NearbyEnemyBuf> buf = NearbyEnemyBufs[NearestEnemiesBufferEntitiesMap[bucket]];
                    for (int j = buf.Length - 1; j >= 0; j--)
                    {
                        if (!L2Ws.Exists(buf[j].Enemy))
                        {
                            //Logger.OnError($"{FRAME}: Remove {bucket} at {j} (len: {buf.Length}): Entity {buf[j].Enemy}");
                            buf.RemoveAt(j); // remove destroyed entities so we can provide a reliable buffer
                        }
                    }

                    if (buf.Length == 0) // there are no active targets left in the zone
                    {
                        // in this case I need to return the buffer to pool
                        NearestEnemiesBufferEntitiesMap.TryGetValue(bucket, out Entity e);
                        BufferEntityPool.Enqueue(e);
                        //Logger.Log($"{FRAME}: Enqueue B. Len {BufferEntityPool.Count}");

                        NearestEnemiesBufferEntitiesMap.Remove(bucket);
                        ActiveZones.Remove(bucket);
                        //Logger.Log($"{FRAME}: Remove: {bucket} Buf len 0.");
                    }
                    //else there are active targets in the zone
                }
            }

            // 2. Add new zones from QueriedZones
            var queriedZoneKeys = QueriedZones.GetKeyArray(Allocator.Temp);
            for (int i = 0; i < queriedZoneKeys.Length; i++)
            {
                int3 bucket = queriedZoneKeys[i];
                if (!ActiveZones.TryGetValue(bucket, out _))
                {
                    // add nearest enemy buffer and activeZones
                    //Logger.Log($"{FRAME}: {bucket} Dequeue.  Len {BufferEntityPool.Count}");
                    NearestEnemiesBufferEntitiesMap.TryAdd(bucket, BufferEntityPool.Dequeue());
                    ActiveZones.TryAdd(bucket, float.MinValue);
                    //Logger.Log($"{FRAME}: Adding {bucket} len: {ActiveZones.Length}");
                    
                }
            }

            var tempKeys = ActiveZones.GetKeyArray(Allocator.Temp);
            Keys.Clear();
            Keys.AddRange(tempKeys);

            for (int i = Keys.Length - 1; i >= 0; i--)
            {
                ActiveZones.TryGetValue(Keys[i], out float lastUpdateTime);
                if (Time - lastUpdateTime < UpdateInterval)
                {
                    //Logger.Log($"{FRAME}: Pruning! lasUpdateTime: {lastUpdateTime}");
                    Keys.RemoveAtSwapBack(i);
                }
                else
                {
                    ActiveZones.Remove(Keys[i]);
                    ActiveZones.TryAdd(Keys[i], Time);
                }
            }
        }
    }

    [BurstCompile]
    private struct ScanForEnemiesJob : IJobParallelForDefer
    {
        private const float ScanRange = 10000f;

        public float Time;
        [ReadOnly] public CollisionWorld CollisionWorld;
        [ReadOnly] public NativeArray<int3> ZoneKeys;
        [ReadOnly] public NativeHashMap<int3, float> ActiveZones;
        [NativeDisableParallelForRestriction] public BufferFromEntity<NearbyEnemyBuf> NearbyEnemyBufs;
        [NativeDisableParallelForRestriction] public NativeHashMap<int3, Entity> NearestEnemiesBufferEntitiesMap;

        public void Execute(int index)
        {
            int3 bucket = ZoneKeys[index];
            NearestEnemiesBufferEntitiesMap.TryGetValue(bucket, out Entity bufferEntity);
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

                NativeList<DistanceHit> hits = new NativeList<DistanceHit>(5, Allocator.Temp);
                var collector = new ClosestHitsCollector<DistanceHit>(pointInput.MaxDistance, hits);
                if (CollisionWorld.CalculateDistance(pointInput, ref collector))
                {
                    for (int i = 0; i < hits.Length; i++)
                    {
                        buf.Add(CollisionWorld.Bodies[hits[i].RigidBodyIndex].Entity);
                    }
                }

                //DistanceHit hit;
                //if (CollisionWorld.CalculateDistance(pointInput, out hit))
                //{
                //    buf.Add(CollisionWorld.Bodies[hit.RigidBodyIndex].Entity);
                //}
            }
        }
    }

    public struct ClosestHitsCollector<T> : ICollector<T> where T : struct, IQueryResult
    {
        public NativeList<T> ClosestHits;

        public bool EarlyOutOnFirstHit => false;

        public float MaxFraction { get; private set; }

        public int NumHits => ClosestHits.Length;

        public ClosestHitsCollector(float maxFraction, NativeList<T> closestHits)
        {
            Assert.IsTrue(closestHits.Capacity > 1);
            MaxFraction = maxFraction;
            ClosestHits = closestHits;
        }

        #region ICollector

        public bool AddHit(T hit)
        {
            Assert.IsTrue(hit.Fraction <= MaxFraction);
            if (NumHits < ClosestHits.Capacity)
            {
                ClosestHits.Add(hit);
            }
            else
            {
                // replace existing furthest hit
                int furthestIndex = 0;
                float furthest = ClosestHits[0].Fraction;
                MaxFraction = -1;
                for (int i = 1; i < NumHits; i++)
                {
                    float frac = ClosestHits[i].Fraction;
                    if (frac > MaxFraction)
                    {
                        if (frac > furthest)
                        {
                            MaxFraction = furthest;
                            furthest = frac;
                            furthestIndex = i;
                        }
                        else
                        {
                            MaxFraction = frac;
                        }
                    }
                }

                ClosestHits.RemoveAtSwapBack(furthestIndex);
                ClosestHits.Add(hit);
                MaxFraction = math.max(hit.Fraction, MaxFraction);
            }

            return true;
        }

        public void TransformNewHits(int oldNumHits, float oldFraction, Math.MTransform transform, uint numSubKeyBits, uint subKey)
        {
            int start = math.min(oldNumHits, NumHits - 1);
            for (int i = start; i < NumHits; i++)
            {
                T hit = ClosestHits[i];
                hit.Transform(transform, numSubKeyBits, subKey);
                ClosestHits[i] = hit;
            }
        }

        public void TransformNewHits(int oldNumHits, float oldFraction, Math.MTransform transform, int rigidBodyIndex)
        {
            int start = math.min(oldNumHits, NumHits - 1);
            for (int i = start; i < NumHits; i++)
            {
                T hit = ClosestHits[i];
                hit.Transform(transform, rigidBodyIndex);
                ClosestHits[i] = hit;
            }
        }

        #endregion
    }

    public struct AllHitsCollector<T> : ICollector<T> where T : struct, IQueryResult
    {
        public bool EarlyOutOnFirstHit => false;
        public float MaxFraction { get; }
        public int NumHits => AllHits.Length;

        public NativeList<T> AllHits;

        public AllHitsCollector(float maxFraction, ref NativeList<T> allHits)
        {
            MaxFraction = maxFraction;
            AllHits = allHits;
        }

        #region

        public bool AddHit(T hit)
        {
            Assert.IsTrue(hit.Fraction < MaxFraction);
            AllHits.Add(hit);
            return true;
        }

        public void TransformNewHits(int oldNumHits, float oldFraction, Math.MTransform transform, uint numSubKeyBits, uint subKey)
        {
            for (int i = oldNumHits; i < NumHits; i++)
            {
                T hit = AllHits[i];
                hit.Transform(transform, numSubKeyBits, subKey);
                AllHits[i] = hit;
            }
        }

        public void TransformNewHits(int oldNumHits, float oldFraction, Math.MTransform transform, int rigidBodyIndex)
        {
            for (int i = oldNumHits; i < NumHits; i++)
            {
                T hit = AllHits[i];
                hit.Transform(transform, rigidBodyIndex);
                AllHits[i] = hit;
            }
        }

        #endregion
    }
}
