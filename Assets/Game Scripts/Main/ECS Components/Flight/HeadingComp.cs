using System;
using Unity.Entities;
using UnityEngine;

[Serializable]
public struct Heading : IComponentData
{
    public float Desired;
    public float Actual;
}

public class HeadingComp : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new Heading());
    }
}