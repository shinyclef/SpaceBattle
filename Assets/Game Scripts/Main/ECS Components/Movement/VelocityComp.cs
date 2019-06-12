using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public struct Velocity : IComponentData
{
    public float Speed;
    public float2 Value;
}

public class VelocityComp : MonoBehaviour, IConvertGameObjectToEntity
{
    public float Speed;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new Velocity() { Speed = Speed });
    }
}