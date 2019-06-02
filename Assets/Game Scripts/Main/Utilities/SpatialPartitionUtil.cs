using System.Runtime.CompilerServices;
using Unity.Mathematics;
using static Unity.Mathematics.math;

public static class SpatialPartitionUtil
{
    private const float NodeSize = 10;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int2 ToSpatialPartition(float2 pos)
    {
        return new int2(round(pos / NodeSize));
    }
}