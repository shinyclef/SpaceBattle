using System;
using Unity.Entities;

[Serializable]
public struct TriggerInfo : IComponentData
{
    public Entity OtherEntity;

    public TriggerInfo(Entity otherEntity)
    {
        OtherEntity = otherEntity;
    }
}