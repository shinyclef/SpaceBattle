using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[DisableAutoCreation]
[UpdateInGroup(typeof(MainGameGroup))]
[UpdateAfter(typeof(HeadingSys))]
public class RotationSysDeprecated : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var job = new Job()
        {
            Dt = Time.deltaTime,
            Time = Time.time
        };

        JobHandle jh = job.Schedule(this, inputDeps);
        return jh;
    }

    /// <summary>
    /// Sets rotation to match the current heading and tilt.
    /// </summary>
    [BurstCompile]
    private struct Job : IJobForEach<Heading, Rotation>
    {
        public float Dt;
        public float Time;

        public void Execute([ReadOnly] ref Heading heading, ref Rotation rot)
        {
            rot.Value = quaternion.AxisAngle(new float3(0, 0, -1), math.radians(heading.CurrentHeading));
        }
    }
}