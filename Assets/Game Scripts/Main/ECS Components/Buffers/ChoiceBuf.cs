using Unity.Entities;
using UnityEngine;

[InternalBufferCapacity(5)]
public struct ChoiceBuf : IBufferElementData
{
    public static implicit operator Choice(ChoiceBuf e) { return e.Value; }
    public static implicit operator ChoiceBuf(Choice e) { return new ChoiceBuf { Value = e }; }

    public Choice Value;
}

public class ChoiceBufComp : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddBuffer<ChoiceBuf>(entity);
    }
}