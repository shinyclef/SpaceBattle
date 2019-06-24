using System;
using Unity.Entities;
using UnityEngine;

[InternalBufferCapacity(6)]
public struct UtilityScoreBuf : IBufferElementData
{
    public static implicit operator float(UtilityScoreBuf e) { return e.Value; }
    public static implicit operator UtilityScoreBuf(float e) { return new UtilityScoreBuf { Value = e }; }

    public float Value;
}

public class UtilityScoreBufComp : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddBuffer<UtilityScoreBuf>(entity);
    }
}