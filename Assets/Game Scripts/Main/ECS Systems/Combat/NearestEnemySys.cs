﻿using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(MainGameGroup))]
[UpdateAfter(typeof(NearestEnemyRequestSys))]
public class NearestEnemySys : JobComponentSystem
{
    private NearestEnemyRequestSysAttempt1 nearestEnemyRequestSys;

    protected override void OnCreate()
    {
        nearestEnemyRequestSys = World.GetOrCreateSystem<NearestEnemyRequestSysAttempt1>();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (!nearestEnemyRequestSys.ZoneTargetBuffers.IsCreated)
        {
            return inputDeps;
        }

        var updateNearestEnemyJob = new UpdateNearestEnemyJob
        {
            Time = Time.time,
            ZoneTargetBuffers = nearestEnemyRequestSys.ZoneTargetBuffers,
            NearbyEnemyBufs = GetBufferFromEntity<NearbyEnemyBuf>(true),
        };

        inputDeps = updateNearestEnemyJob.Schedule(this, inputDeps);
        return inputDeps;
    }

    [BurstCompile]
    private struct UpdateNearestEnemyJob : IJobForEach<LocalToWorld, Faction, NearestEnemy>
    {
        public float Time;
        [ReadOnly] public NativeHashMap<int3, Entity> ZoneTargetBuffers;
        [ReadOnly] public BufferFromEntity<NearbyEnemyBuf> NearbyEnemyBufs;

        public void Execute([ReadOnly] ref LocalToWorld l2w, [ReadOnly] ref Faction faction, ref NearestEnemy enemy)
        {
            if (!enemy.UpdatePending)
            {
                return;
            }

            int2 zone = SpatialPartitionUtil.ToSpatialPartition(l2w.Position.xy);
            int factionInt = (int)faction.Value; 
            int3 bucket = new int3(zone.x, zone.y, factionInt);
            if (ZoneTargetBuffers.TryGetValue(bucket, out Entity bufEntity) &&
                bufEntity != Entity.Null && NearbyEnemyBufs.Exists(bufEntity))
            {
                DynamicBuffer<NearbyEnemyBuf> buf = NearbyEnemyBufs[bufEntity];
                if (buf.Length > 0)
                {
                    enemy.Entity = buf[0];
                    enemy.LastUpdatedTime = Time;
                    enemy.UpdatePending = false;
                    return;
                }

                //Logger.Log("Len == 0"); // TODO: Cleanup
            }
            else
            {
                // TODO: Cleanup
                //Logger.Log($"TryGet: {ZoneTargetBuffers.TryGetValue(bucket, out Entity bufEntity2)}, NotNull: {bufEntity != Entity.Null}, Exists: {NearbyEnemyBufs.Exists(bufEntity)}");
            }

            enemy.Entity = Entity.Null;
        }
    }
}