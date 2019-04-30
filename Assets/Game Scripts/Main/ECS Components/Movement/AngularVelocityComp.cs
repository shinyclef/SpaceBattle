using System;
using Unity.Entities;
using UnityEngine;

[Serializable]
public struct AngularVelocity : IComponentData
{
    public float CurrentVelocity;
    public float Acceleration;
    public float MaxSpeed;
}

public class AngularVelocityComp : MonoBehaviour, IConvertGameObjectToEntity
{
    public float Acceleration = 50f;
    public float MaxTurnSpeed = 50f;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new AngularVelocity { Acceleration = Acceleration, MaxSpeed = MaxTurnSpeed });
    }
}