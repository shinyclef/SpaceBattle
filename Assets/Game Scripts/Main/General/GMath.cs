using System.Runtime.CompilerServices;
using Unity.Mathematics;
using static Unity.Mathematics.math;

public static class gmath
{
    private static float3 forward = new float3(0, 0, 1);
    private static float3 up = new float3(0, 1, 0);
    private static float3 right = new float3(1, 0, 0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float SignedInnerAngle(float from, float to)
    {
        float res = to - from;
        res = (MathMod((res + 180f), 360f)) - 180f;
        return res;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ToAngleRange360(float a)
    {
        return (a + 360f) % 360f;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float MathMod(float x, float n)
    {
        return x - floor(x / n) * n;
    }
}