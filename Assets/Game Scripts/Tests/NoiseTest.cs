using NUnit.Framework;
using System.Diagnostics;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;

namespace Tests
{
    public class NoiseTest : AssertionHelper
    {
        [Test]
        public void CNoiseTest()
        {
            var sw = new Stopwatch();
            var job = new CNoiseJob();
            sw.Start();
            JobHandle jh = job.Schedule();
            jh.Complete();
            sw.Stop();
            Logger.Log($"CNoise: {sw.Elapsed.TotalMilliseconds}ms");
        }

        //[BurstCompile]
        private struct CNoiseJob : IJob
        {
            public float F;

            public void Execute()
            {
                for (int i = 0; i < 100000; i++)
                {
                    F = noise.cnoise(new float2(i, i));
                }
            }
        }

        [Test]
        public void SNoiseTest()
        {
            var sw = new Stopwatch();
            var job = new SNoiseJob();
            sw.Start();
            JobHandle jh = job.Schedule();
            jh.Complete();
            sw.Stop();
            Logger.Log($"SNoise: {sw.Elapsed.TotalMilliseconds}ms");
        }

        //[BurstCompile]
        private struct SNoiseJob : IJob
        {
            public float F;

            public void Execute()
            {
                for (int i = 0; i < 100000; i++)
                {
                    F = noise.snoise(new float2(i, i));
                }
            }
        }

        [Test]
        public void FastNoiseTest()
        {
            var sw = new Stopwatch();
            var job = new FastNoiseJob();
            sw.Start();
            JobHandle jh = job.Schedule();
            jh.Complete();
            sw.Stop();
            Logger.Log($"Fast: {sw.Elapsed.TotalMilliseconds}ms. Min: {job.Min}, Max: {job.Max}.");
        }

        //[BurstCompile]
        private struct FastNoiseJob : IJob
        {
            public float Min;
            public float Max;

            public void Execute()
            {
                Min = float.MaxValue;
                Max = float.MinValue;
                for (int i = 0; i < 60; i++)
                {
                    float val = gmath.FastNoise(i * 0.05f, 0);
                    Logger.Log($"{i}: {val}");
                }
            }
        }

        [Test]
        public void RemapTest()
        {
            Expect(math.remap(0f, 127f, 0f, 1f, 0f) == 0f);
            Expect(math.remap(0f, 127f, 0f, 1f, 127f) == 1f);
            Logger.Log(math.remap(0f, 127f, 0f, 1f, 63f));
            Logger.Log(math.remap(0f, 127f, 0f, 1f, 63.5f));
            Logger.Log(math.remap(0f, 127f, 0f, 1f, 64f));
        }
    }
}