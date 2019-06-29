using NUnit.Framework;
using System.Collections.Generic;
using Unity.Mathematics;

namespace Tests
{
    public class HeavingVsFloat2 : AssertionHelper
    {
        [Test]
        public void KeysTest()
        {
            var headings = new List<float>()
            {
                0f,
                90f,
                180f,
                270f,
                360f
            };

            Logger.Log("-----------------");
            Logger.Log("Heading to Float2");
            Logger.Log("-----------------");
            foreach (var h in headings)
            {
                float2 res = gmath.HeadingToFloat2(h);
                float backAgain = math.round(gmath.Float2ToHeading(res) * 100f) / 100f;
                Logger.Log($"{h} -> {math.round(res * 100f) / 100f} -> {backAgain}");
            }
        }
    }
}