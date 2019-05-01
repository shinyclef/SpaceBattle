using System;
using Unity.Entities;
using UnityEngine;

[Serializable]
public struct SpawnTime : IComponentData
{
    public float Value;

    public SpawnTime(float value)
    {
        Value = value;
    }
}

public class SpawnTimeComp : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new SpawnTime());
    }
}