using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(MainGameGroup))]
[UpdateAfter(typeof(NearestEnemySys))]
public class CombatTargetSys : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var job = new Job
        {
            Entities = GetComponentDataFromEntity<LocalToWorld>(),
            Time = Time.time
        };

        JobHandle jh = job.Schedule(this, inputDeps);
        return jh;
    }

    [BurstCompile]
    private struct Job : IJobForEachWithEntity<NearestEnemy, CombatTarget>
    {
        [ReadOnly] public ComponentDataFromEntity<LocalToWorld> Entities;
        public float Time;

        public void Execute(Entity entity, int index, [ReadOnly] ref NearestEnemy nearestEnemy, ref CombatTarget target)
        {
            if (target.Value == nearestEnemy.Entity)
            {
                return;
            }

            // switch to nearest enemy if existing target is gone, or enough time has passed
            if (!Entities.Exists(target.Value) || Time - target.AcquiredTime > 5f)
            {
                target.Value = nearestEnemy.Entity;
                target.AcquiredTime = Time;
                return;
            }
        }
    }
}