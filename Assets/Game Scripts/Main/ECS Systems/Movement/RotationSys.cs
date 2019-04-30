using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

[UpdateInGroup(typeof(GameGroupPrePhysics))]
[UpdateAfter(typeof(VelocitySys))]
public class RotationSys : JobComponentSystem
{
    /// <summary>
    /// Sets rotation to match the current heading and tilt.
    /// </summary>
    [BurstCompile]
    private struct Job : IJobForEach<Heading, Rotation>
    {
        [NativeDisableParallelForRestriction]
        [DeallocateOnJobCompletion]
        public NativeArray<Random> Rngs;
        public float Dt;
        public float Time;

        [NativeSetThreadIndex]
        private int threadIndex;

        public void Execute([ReadOnly] ref Heading heading, ref Rotation rot)
        {
            rot.Value = quaternion.AxisAngle(new float3(0, 0, -1), math.radians(heading.CurrentHeading));
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        NativeArray<Random> rngs = new NativeArray<Random>(Environment.ProcessorCount, Allocator.TempJob);
        for (int i = 0; i < rngs.Length; i++)
        {
            rngs[i] = new Random(Rand.New().NextUInt());
        }

        var job = new Job()
        {
            Rngs = rngs,
            Dt = Time.deltaTime,
            Time = Time.time
        };

        JobHandle jh = job.Schedule(this, inputDeps);
        return jh;
    }
}