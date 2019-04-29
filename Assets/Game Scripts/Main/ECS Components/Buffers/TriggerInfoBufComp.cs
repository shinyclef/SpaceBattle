using Unity.Entities;
using UnityEngine;

[InternalBufferCapacity(1)]
public struct TriggerInfoBuf : IBufferElementData
{
    public static implicit operator TriggerInfo(TriggerInfoBuf e) { return e.Value; }
    public static implicit operator TriggerInfoBuf(TriggerInfo e) { return new TriggerInfoBuf { Value = e }; }

    public TriggerInfo Value;
}

public class TriggerInfoBufComp : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddBuffer<TriggerInfoBuf>(entity);
    }
}