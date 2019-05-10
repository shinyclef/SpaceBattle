using Unity.Entities;
using UnityEngine;

[InternalBufferCapacity(5)]
public struct ConsiderationIndexBuf : IBufferElementData
{
    public static implicit operator int(ConsiderationIndexBuf e) { return e.Value; }
    public static implicit operator ConsiderationIndexBuf(int e) { return new ConsiderationIndexBuf { Value = e }; }

    public int Value;
}

public class ConsiderationIndexBufComp : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddBuffer<ChoiceBuf>(entity);
    }
}