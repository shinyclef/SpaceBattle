using System;
using Unity.Entities;
using UnityEngine;

[Serializable]
public struct DamageHealthOnTrigger : IComponentData
{
    public float Value;
}

public class DamageHealthOnTriggerComp : MonoBehaviour, IConvertGameObjectToEntity
{
    public float Value;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new DamageHealthOnTrigger() { Value = Value });
    }
}