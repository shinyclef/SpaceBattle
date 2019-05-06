using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

[UpdateInGroup(typeof(MainGameGroup))]
[UpdateAfter(typeof(NearestEnemySys))]
public class CombatTargetSys : JobComponentSystem
{
    [BurstCompile]
    private struct Job : IJobForEach<NearestEnemy, CombatTarget>
    {
        public void Execute([ReadOnly] ref NearestEnemy nearestEnemy, [ReadOnly] ref CombatTarget target)
        {
            target.Value = nearestEnemy.Entity;
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var job = new Job();
        JobHandle jh = job.Schedule(this, inputDeps);
        return jh;
    }
}