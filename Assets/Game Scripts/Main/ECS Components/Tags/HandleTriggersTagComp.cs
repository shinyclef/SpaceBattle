using System;
using Unity.Entities;
using UnityEngine;

[Serializable]
public struct HandleTriggersTag : IComponentData { }

public class HandleTriggersTagComp : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponent(entity, typeof(HandleTriggersTag));
    }
}