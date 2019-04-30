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
    private struct Job : IJobForEach<NearestEnemy, MoveDestination>
    {
        [ReadOnly] public ComponentDataFromEntity<Translation> TranslationComps;

        public void Execute([ReadOnly] ref NearestEnemy enemy, ref MoveDestination dest)
        {
            if (TranslationComps.Exists(enemy.Entity))
            {
                dest.Value = TranslationComps[enemy.Entity].Value.xy;
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