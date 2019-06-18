using System.Runtime.CompilerServices;
using Unity.Mathematics;

public struct TargetLeadHelper
{
    //--------------//
    //-- Method 1 --//
    //--------------//

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float2 DirectSolution(float2 pos, float2 vel, float2 targetPos, float2 targetVel, float projSpeed)
    {
        float a = math.dot(targetVel, targetVel) - projSpeed * projSpeed;
        float b = 2.0f * math.dot(targetPos, targetVel);
        float c = math.dot(targetPos, targetPos);

        float t = FirstPositiveSoluationOfQuadraticEquation(a, b, c);
        if (t <= 0.0f)
        {
            return pos; // Indicate we failed to find a solution
        }

        return targetPos + t * targetVel;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float FirstPositiveSoluationOfQuadraticEquation(float a, float b, float c)
    {
        float discriminant = b * b - 4.0f * a * c;
        if (discriminant < 0.0f)
        {
            return -1.0f; // Indicate there is no solution                                                                      
        }
            
        float s = math.sqrt(discriminant);
        float x1 = (-b - s) / (2.0f * a);
        if (x1 > 0.0f)
        {
            return x1;
        }
            
        float x2 = (-b + s) / (2.0f * a);
        if (x2 > 0.0f)
        {
            return x2;
        }
            
        return -1.0f; // Indicate there is no positive solution                                                               
    }

    //--------------//
    //-- Method 2 --//
    //--------------//

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float2 FirstOrderIntercept(float2 pos, float2 vel, float2 targetPos, float2 targetVel, float projSpeed)
    {
        float2 targetRelativePosition = targetPos - pos;
        float2 targetRelativeVelocity = targetVel - vel;
        float t = FirstOrderInterceptTime(projSpeed, targetRelativePosition, targetRelativeVelocity);
        return targetPos + t * targetRelativeVelocity;
    }

    //first-order intercept using relative target position
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float FirstOrderInterceptTime(float projSpeed, float2 targetRelativePos, float2 targetRelativeVel)
    {
        float velocitySquared = gmath.MagnitudeSqr(targetRelativeVel);
        if (velocitySquared < 0.001f)
        {
            return 0f;
        }

        float a = velocitySquared - projSpeed * projSpeed;

        //handle similar velocities
        if (math.abs(a) < 0.001f)
        {
            float t = gmath.MagnitudeSqr(-targetRelativePos) / (2f * math.dot(targetRelativeVel, targetRelativePos));
            return math.max(t, 0f); //don't shoot back in time
        }

        float b = 2f * math.dot(targetRelativeVel, targetRelativePos);
        float c = gmath.MagnitudeSqr(targetRelativePos);
        float determinant = b * b - 4f * a * c;
        if (determinant > 0f)
        { 
            //determinant > 0; two intercept paths (most common)
            float t1 = (-b + math.sqrt(determinant)) / (2f * a), t2 = (-b - math.sqrt(determinant)) / (2f * a);
            if (t1 > 0f)
            {
                if (t2 > 0f)
                {
                    return math.min(t1, t2); //both are positive
                }
                else
                {
                    return t1; //only t1 is positive
                }
            }
            else
            {
                return math.max(t2, 0f); //don't shoot back in time
            }
        }
        else if (determinant < 0f) //determinant < 0; no intercept path
        {
            return 0f;
        }
        else //determinant = 0; one intercept path, pretty much never happens
        {
            return math.max(-b / (2f * a), 0f); //don't shoot back in time
        }
    }


    //---------------//
    //-- Iterative --//
    //---------------//

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float2 GetTargetLeadHitPosIterative(float2 pos, float2 vel, float2 targetPos, float2 targetVel, float2 targetAccel, float projSpeed)
    {
        const float Epsilon = 0.05f;
        float t = math.distance(pos, targetPos) / projSpeed;
        float oldT = 0f;
        float2 tPos = targetPos;
        int i = 0;
        while (math.abs(t - oldT) > Epsilon)
        {
            oldT = t;
            tPos = targetPos + (targetVel - vel) * t + (0.5f * targetAccel * (t * t));
            t = math.distance(pos, tPos) / projSpeed;
            i++;
        }

        Logger.LogIf(i > 2, $"{i} iterations");

        return tPos;
    }
}