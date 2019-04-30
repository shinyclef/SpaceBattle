using System;
using Unity.Entities;
using UnityEngine;

[Serializable]
public struct Health : IComponentData
{
    public float Value;
}

public class HealthComp : MonoBehaviour, IConvertGameObjectToEntity
{
    public float Value;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new Health() { Value = Value });
    }
}