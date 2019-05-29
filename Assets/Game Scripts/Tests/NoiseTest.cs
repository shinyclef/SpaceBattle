using NUnit.Framework;
using System.Diagnostics;
using Unity.Mathematics;

namespace Tests
{
    public class NoiseTest : AssertionHelper
    {
        [Test]
        public void CNoiseTest()
        {
            noise.cnoise(new float2(0, 0));

            var sw = new Stopwatch();
            sw.Start();
            for (int i = 0; i < 10000; i++)
            {
                noise.cnoise(new float2(i, i));
            }
            sw.Stop();
            Logger.Log($"CNoise: {sw.Elapsed.TotalMilliseconds}");
        }

        [Test]
        public void SNoiseTest()
        {
            noise.snoise(new float2(0, 0));

            var sw = new Stopwatch();
            sw.Start();
            for (int i = 0; i < 10000; i++)
            {
                noise.snoise(new float2(i, i));
            }
            sw.Stop();
            Logger.Log($"SNoise: {sw.Elapsed.TotalMilliseconds}");
        }
    }
}
