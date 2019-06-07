using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(MainGameGroup))]
[UpdateAfter(typeof(RotationSys))]
public class VelocitySys : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var job = new Job()
        {
            Dt = Time.deltaTime
        };

        JobHandle jh = job.Schedule(this, inputDeps);
        return jh;
    }

    [BurstCompile]
    private struct Job : IJobForEach<Rotation, Velocity>
    {
        public float Dt;

        public void Execute([ReadOnly] ref Rotation rot, ref Velocity vel)
        {
            vel.Value = math.mul(rot.Value, new float3(0, 1, 0)).xy * vel.Speed;
        }
    }
}