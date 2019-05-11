﻿using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

// Applies velocity to current position.
[UpdateInGroup(typeof(MainGameGroup))]
[UpdateAfter(typeof(RotationSys))]
public class MovementSys : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var job = new Job()
        {
            dt = Time.deltaTime
        };

        JobHandle jh = job.Schedule(this, inputDeps);
        return jh;
    }

    [BurstCompile]
    private struct Job : IJobForEach<Translation, Velocity>
    {
        public float dt;

        public void Execute(ref Translation translation, [ReadOnly] ref Velocity vel)
        {
            translation.Value += new float3(vel.Value, 0) * dt;
        }
    }
}