using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public struct Thrust : IComponentData
{
    public half Acceleration;
    public half MaxSpeed;
    public half AngularAcceleration;
    public half AngularMaxSpeed;
    public float2 CurrentAcceleration;

    public Thrust(half acceleration, half maxSpeed, half angularAcceleration, half angularMaxSpeed)
    {
        Acceleration = acceleration;
        MaxSpeed = maxSpeed;
        AngularAcceleration = angularAcceleration;
        AngularMaxSpeed = angularMaxSpeed;
        CurrentAcceleration = float2.zero;
    }
}

public class ThrustComp : MonoBehaviour, IConvertGameObjectToEntity
{
    public float Acceleration;
    public float MaxSpeed;
    public float AngularAcceleration;
    public float AngularMaxSpeed;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new Thrust(new half(Acceleration), new half(MaxSpeed), new half(AngularAcceleration), new half(AngularMaxSpeed)));
    }
}