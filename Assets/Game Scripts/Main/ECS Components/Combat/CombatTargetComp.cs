using System;
using Unity.Entities;
using UnityEngine;

[Serializable]
public struct CombatTarget : IComponentData
{
    public Entity Value;
    public float AcquiredTime;
}

public class CombatTargetComp : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new CombatTarget());
    }
}