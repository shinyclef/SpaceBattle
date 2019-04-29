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
[UpdateAfter(typeof(ShipSpawnerSys))]
public class RotationSys : JobComponentSystem
{
    [BurstCompile]
    private struct Job : IJobForEachWithEntity<Rotation, Velocity, SpawnTime>
    {
        [NativeDisableParallelForRestriction]
        [DeallocateOnJobCompletion]
        public NativeArray<Random> Rngs;
        public float Dt;
        public float Time;

        [NativeSetThreadIndex]
        private int threadIndex;

        public void Execute(Entity entity, int index, ref Rotation rot, [ReadOnly] ref Velocity vel, [ReadOnly] ref SpawnTime st)
        {
            //Random rand = Rngs[threadIndex - 1];
            //float sign = math.floor(rand.NextFloat() - 0.5f) * 2 + 1;
            //float amplitude = rand.NextFloat();
            //float waveLen = rand.NextFloat();
            //float lifeTime = Time - st.Value;
            //float rotationDegrees = math.cos(lifeTime) * sign * 0.5f * Dt;
            //rot.Value = math.mul(math.normalize(rot.Value), quaternion.AxisAngle(new float3(0, 0, -1), rotationDegrees));
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