using System;
using Unity.Entities;
using UnityEngine;

[Serializable]
public struct DoSpatialPartitionTag : IComponentData
{
}

public class DoSpatialPartitionTagComp : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new DoSpatialPartitionTag());
    }
}