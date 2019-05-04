using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;

// Applies velocity to current position.
[UpdateInGroup(typeof(GameGroupPostPhysics))]
[UpdateAfter(typeof(CombatTargetSys))]
public class MoveDestinationSys : JobComponentSystem
{
    [BurstCompile]
    private struct Job : IJobForEach<CombatTarget, Translation, Heading, MoveDestination>
    {
        [ReadOnly] public ComponentDataFromEntity<Translation> TranslationComps;

        public void Execute([ReadOnly] ref CombatTarget enemy, [ReadOnly] ref Translation tran, [ReadOnly] ref Heading heading, ref MoveDestination dest)
        {
            if (enemy.Value != Entity.Null && TranslationComps.Exists(enemy.Value))
            {
                dest.Value = TranslationComps[enemy.Value].Value.xy;
                dest.IsCombatTarget = true;
            }
            else
            {
                dest.Value = tran.Value.xy + Heading.ToFloat2(heading.CurrentHeading);
                dest.IsCombatTarget = false;
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