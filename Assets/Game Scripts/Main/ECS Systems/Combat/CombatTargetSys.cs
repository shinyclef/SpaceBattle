﻿using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(MainGameGroup))]
[UpdateAfter(typeof(NearestEnemySys))]
public class CombatTargetSys : JobComponentSystem
{
    private const float CommitToTargetTime = 3f;

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var job = new Job
        {
            L2WComps = GetComponentDataFromEntity<LocalToWorld>(true),
            Time = Time.time
        };

        JobHandle jh = job.Schedule(this, inputDeps);
        return jh;
    }

    [BurstCompile]
    private struct Job : IJobForEachWithEntity<NearestEnemy, CombatTarget>
    {
        [ReadOnly] public ComponentDataFromEntity<LocalToWorld> L2WComps;
        public float Time;
        public void Execute(Entity entity, int index, [ReadOnly] ref NearestEnemy nearestEnemy, ref CombatTarget target)
        {
            bool targetExists = target.Entity != Entity.Null && L2WComps.Exists(target.Entity);
            bool newTargetRequired = !targetExists || Time - target.AcquiredTime > CommitToTargetTime;
            
            if (newTargetRequired)
            {
                if (nearestEnemy.LastUpdatedTime == Time)
                {
                    target.Entity = nearestEnemy.Entity;
                    target.AcquiredTime = Time;
                }
                else if (!nearestEnemy.UpdatePending)
                {
                    nearestEnemy.UpdatePending = true;
                }
            }

            targetExists = target.Entity != Entity.Null && L2WComps.Exists(target.Entity);
            if (targetExists)
            {
                LocalToWorld targetL2W = L2WComps[target.Entity];
                target.Pos = targetL2W.Position.xy;
                target.Heading = Heading.FromFloat2(targetL2W.Up.xy);
            }
            else
            {
                target.Entity = Entity.Null;
            }
        }
    }
}