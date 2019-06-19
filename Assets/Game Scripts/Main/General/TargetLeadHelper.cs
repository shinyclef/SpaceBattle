using System.Runtime.CompilerServices;
using Unity.Mathematics;

public struct TargetLeadHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float2 GetTargetLeadHitPosIterative(float2 pos, float2 vel, float2 targetPos, float2 targetVel, float2 targetAccel, float projSpeed)
    {
        const float Epsilon = 0.05f;
        const int MaxIterations = 10;

        float t = math.distance(pos, targetPos) / projSpeed;
        float oldT = 0f;
        float2 tPos = targetPos;
        int i = 0;
        while (math.abs(t - oldT) > Epsilon && i < MaxIterations)
        {
            oldT = t;
            tPos = targetPos + (targetVel - vel) * t + (0.5f * targetAccel * (t * t));
            t = math.distance(pos, tPos) / projSpeed;
            i++;
        }

        tPos += targetVel * Epsilon * 0.5f;
        //Logger.LogIf(i > 4, $"{i} iterations");
        return tPos;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float2 GetTargetLeadHitPosIterativeRough(float2 pos, float2 vel, float2 targetPos, float2 targetVel, float2 targetAccel, float projSpeed)
    {
        float t1 = math.distance(pos, targetPos) / projSpeed;
        float2 tPos = targetPos + (targetVel - vel) * t1 + (0.5f * targetAccel * (t1 * t1));
        float t2 = math.distance(pos, tPos) / projSpeed;
        float ratio = (t2 - t1) / t1 * 2f;

        t1 += ratio * t1;
        tPos = targetPos + (targetVel - vel) * t1 + (0.5f * targetAccel * (t1 * t1));

        return tPos;
    }
}