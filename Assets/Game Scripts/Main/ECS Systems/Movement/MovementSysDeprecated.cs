using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[DisableAutoCreation]
[UpdateInGroup(typeof(MainGameGroup))]
[UpdateAfter(typeof(RotationSysDeprecated))]
public class MovementSysDeprecated : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        inputDeps =  new Job()
        {
            Dt = Time.deltaTime
        }.Schedule(this, inputDeps);

        return inputDeps;
    }

    [BurstCompile]
    private struct Job : IJobForEach<Velocity, Translation>
    {
        public float Dt;

        public void Execute([ReadOnly] ref Velocity vel, ref Translation translation)
        {
            translation.Value += new float3(vel.Value, 0) * Dt;
        }
    }
}