using System.Runtime.CompilerServices;
using Unity.Mathematics;
using static Unity.Mathematics.math;

public static class MathExtensions
{
    private static float3 forward = new float3(0, 0, 1);
    private static float3 up = new float3(0, 1, 0);
    private static float3 right = new float3(1, 0, 0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float3 Forward(this quaternion q)
    {
        return mul(q, forward);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float3 Up(this quaternion q)
    {
        return mul(q, up);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float3 Right(this quaternion q)
    {
        return mul(q, right);
    }

    public static float3 LocalToWorldPos(this float3 localPos, quaternion rot)
    {
        return mul(rot, right) * localPos.x +
               mul(rot, up) * localPos.y +
               mul(rot, forward) * localPos.z;
    }
}
