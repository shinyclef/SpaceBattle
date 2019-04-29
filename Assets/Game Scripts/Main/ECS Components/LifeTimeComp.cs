using System;
using Unity.Entities;
using UnityEngine;

[Serializable]
public struct LifeTime : IComponentData
{
    public float Value;
}

public class LifeTimeComp : MonoBehaviour, IConvertGameObjectToEntity
{
    public float Value;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new LifeTime() { Value = Value });
    }
}