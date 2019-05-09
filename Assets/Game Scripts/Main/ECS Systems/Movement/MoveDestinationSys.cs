using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(MainGameGroup))]
[UpdateAfter(typeof(CombatTargetSys))]
public class MoveDestinationSys : JobComponentSystem
{
    [BurstCompile]
    private struct Job : IJobForEach<CombatTarget, LocalToWorld, Heading, MoveDestination>
    {
        [ReadOnly] public ComponentDataFromEntity<Translation> TranslationComps;

        public void Execute([ReadOnly] ref CombatTarget enemy, [ReadOnly] ref LocalToWorld l2w, [ReadOnly] ref Heading heading, ref MoveDestination dest)
        {
            if (enemy.Value != Entity.Null && TranslationComps.Exists(enemy.Value))
            {
                dest.Value = CombatFlyToward(TranslationComps[enemy.Value].Value.xy);
                dest.IsCombatTarget = true;
            }
            else
            {
                dest.Value = l2w.Position.xy + l2w.Up.xy;
                dest.IsCombatTarget = false;
            }
            
            float2 CombatFlyToward(float2 enemyPos)
            {
                return enemyPos;
            }

            float2 CombatFlyAway(float2 enemyPos)
            {
                return enemyPos;
            }
        }        
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var job = new Job()
        {
            TranslationComps = GetComponentDataFromEntity<Translation>()
        };

        JobHandle jh = job.Schedule(this, inputDeps);
        return jh;
    }
}