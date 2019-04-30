using System;
using Unity.Entities;
using UnityEngine;

[Serializable]
public struct Heading : IComponentData
{
    // Note: Heading is stored in degrees.
    public float CurrentHeading;
    public float TargetHeading;
}

public class HeadingComp : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new Heading());
    }
}