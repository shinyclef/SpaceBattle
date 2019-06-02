using NUnit.Framework;
using System.Collections.Generic;
using Unity.Mathematics;

namespace Tests
{
    public class SpatialPartitionTest : AssertionHelper
    {
        [Test]
        public void ToSpatialPartitionTest()
        {
            var pairs = new Dictionary<float2, int2>()
            {
                {new float2(0f, 0f), new int2(0, 0) },
                {new float2(4f, -4f), new int2(0, 0) },
                {new float2(6f, -6f), new int2(1, -1) },
                {new float2(14.9f, -14.9f), new int2(1, -1) },
                {new float2(15f, -15f), new int2(2, -2) },
                {new float2(73f, 93f), new int2(7, 9) },
            };

            foreach (KeyValuePair<float2, int2> pair in pairs)
            {
                var val = SpatialPartitionUtil.ToSpatialPartition(pair.Key);
                Expect(val.Equals(pair.Value), $"Key: {pair.Key}, Value: {val}, Expected: {pair.Value}");
            }
        }
    }
}