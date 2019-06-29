using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;

[DisableAutoCreation]
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
            NearbyEnemyBufs = GetBufferFromEntity<NearbyEnemyBuf>(true),
            Time = Time.time
        };

        JobHandle jh = job.Schedule(this, inputDeps);
        return jh;
    }

    [BurstCompile]
    private struct Job : IJobForEachWithEntity<NearestEnemy, CombatTarget>
    {
        public float Time;
        [ReadOnly] public ComponentDataFromEntity<LocalToWorld> L2WComps;
        [ReadOnly] public BufferFromEntity<NearbyEnemyBuf> NearbyEnemyBufs;

        public void Execute(Entity entity, int index, [ReadOnly] ref NearestEnemy nearestEnemy, ref CombatTarget target)
        {
            bool targetExists = target.Entity != Entity.Null && L2WComps.Exists(target.Entity);
            bool newTargetRequired = !targetExists || Time - nearestEnemy.LastUpdatedTime > CommitToTargetTime;
            
            if (newTargetRequired)
            {
                if (nearestEnemy.LastUpdatedTime == Time)
                {
                    //target.Entity = nearestEnemy.BufferEntity;
                }
                else
                {
                    // check if all candidates are gone and we need new candidates
                    DynamicBuffer<NearbyEnemyBuf> buf = NearbyEnemyBufs[nearestEnemy.BufferEntity];
                    if (buf.Length == 0 && !nearestEnemy.UpdateRequired)
                    {
                        nearestEnemy.UpdateRequired = true;
                    }
                }
            }

            targetExists = target.Entity != Entity.Null && L2WComps.Exists(target.Entity);
            if (targetExists)
            {
                //LocalToWorld targetL2W = L2WComps[target.Entity];
                //target.Pos = targetL2W.Position.xy;
            }
            else
            {
                target.Entity = Entity.Null;
            }
        }
    }
}