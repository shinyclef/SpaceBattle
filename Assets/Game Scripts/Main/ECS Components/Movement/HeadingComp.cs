using System;
using System.Runtime.CompilerServices;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public struct Heading : IComponentData
{
    // Note: Heading is stored in degrees.
    public float CurrentHeading;
    public float TargetHeading;

    public Heading(float heading)
    {
        CurrentHeading = heading;
        TargetHeading = heading;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static quaternion ToQuaternion(float heading)
    {
        return quaternion.RotateZ(heading);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float2 ToFloat2(float heading)
    {
        return quaternion.EulerXYZ(0f, 0f, heading).Up().xy;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float FromQuaternion(quaternion q)
    {
        float4 v = q.value;

        //// yaw (z-axis)
        float siny_cosp = 2.0f * (v.w * v.z + v.x * v.y);
        float cosy_cosp = 1.0f - 2.0f * (v.y * v.y + v.z * v.z);
        return -math.degrees(math.atan2(siny_cosp, cosy_cosp));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float FromFloat2(float2 dir)
    {
        return gmath.ToAngleRange360(math.degrees(math.atan2(dir.x, dir.y)));
    }
}

public class HeadingComp : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new Heading());
    }
}