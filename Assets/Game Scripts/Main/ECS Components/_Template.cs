using System;
using Unity.Entities;
using UnityEngine;

[Serializable]
public struct Template : IComponentData
{
    public float Value;
}

public class WingmateComp : MonoBehaviour, IConvertGameObjectToEntity
{
    public float Value;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new Template() { Value = Value });
    }
}