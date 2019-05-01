using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;

// Applies velocity to current position.
[UpdateInGroup(typeof(GameGroupPrePhysics))]
[UpdateAfter(typeof(RotationSys))]
public class MoveDestinationSys : JobComponentSystem
{
    [BurstCompile]
    private struct Job : IJobForEach<NearestEnemy, Translation, Heading, MoveDestination>
    {
        [ReadOnly] public ComponentDataFromEntity<Translation> TranslationComps;

        public void Execute([ReadOnly] ref NearestEnemy enemy, [ReadOnly] ref Translation tran, [ReadOnly] ref Heading heading, ref MoveDestination dest)
        {
            if (enemy.Entity != Entity.Null && TranslationComps.Exists(enemy.Entity))
            {
                dest.Value = TranslationComps[enemy.Entity].Value.xy;
            }
            else
            {
                dest.Value = tran.Value.xy + Heading.ToFloat2(heading.CurrentHeading);
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