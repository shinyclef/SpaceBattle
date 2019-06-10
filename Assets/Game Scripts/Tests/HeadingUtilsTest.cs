using NUnit.Framework;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;

namespace Tests
{
    public class HeadingUtilsTest : AssertionHelper
    {
        [Test]
        public void KeysTest()
        {
            var pairs = new Dictionary<float, float2>
            {
                { 0f, new float2(0f, 1f) },
                { 90f, new float2(-1f, 0f) },
                { 180f, new float2(0f, -1f) },
                { 270f, new float2(1f, 0f) },
                { 360f, new float2(0f, 1f) },
            };

            foreach (var pair in pairs)
            {
                float2 res = gmath.HeadingToFloat2(pair.Key);
                Expect(res.x == pair.Value.x, $"heading: {pair.Key}, vector: {res}, expected: {pair.Value}");
                Expect(res.y == pair.Value.y, $"heading: {pair.Key}, vector: {res}, expected: {pair.Value}");
            }
        }
    }
}