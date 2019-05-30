using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public struct CombatTarget : IComponentData
{
    // Note: Check if Entity is Entity.Null before trying to use the other properties.
    public Entity Entity;
    public float2 Pos;
    public float Heading;
    public float AcquiredTime;
}

public class CombatTargetComp : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new CombatTarget());
    }
}