﻿using System.Runtime.CompilerServices;
using Unity.Mathematics;
using static Unity.Mathematics.math;

public struct SpatialPartitionUtil
{
    private const int NodeSize = 20;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int2 ToSpatialPartition(float2 pos)
    {
        return new int2(round(pos / NodeSize)) * NodeSize;
    }
}