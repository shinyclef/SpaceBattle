using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public struct MoveDestination : IComponentData
{
    public float2 Value;
    public bool IsCombatTarget;

    public MoveDestination(float2 value, bool isCombatTarget)
    {
        Value = value;
        IsCombatTarget = isCombatTarget;
    }
}

public class MoveDestinationComp : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new MoveDestination());
    }
}