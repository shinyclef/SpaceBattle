using System;
using Unity.Entities;
using UnityEngine;

[Serializable]
public struct RotationBankAnim : IComponentData
{
    public float MaxBankDegrees;
    public float MaxBankAngularVelocity;
    public float Smoothing;
    public Entity ModelEntity;
    public float CurrentRot;

    public RotationBankAnim(float maxBankDegrees, float maxBankAngularVelocity, float smoothing, Entity modelEntity)
    {
        MaxBankDegrees = maxBankDegrees;
        MaxBankAngularVelocity = maxBankAngularVelocity;
        Smoothing = smoothing;
        ModelEntity = modelEntity;
        CurrentRot = 0f;
    }
}

public class RotationBankAnimComp : MonoBehaviour, IConvertGameObjectToEntity
{
    public float MaxBankDegrees;
    public float MaxBankAngularVelocity;
    public float Smoothing;
    public GameObject ModelEntity;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        Entity modelEntity = conversionSystem.GetPrimaryEntity(ModelEntity);
        dstManager.AddComponentData(entity, new RotationBankAnim(MaxBankDegrees, MaxBankAngularVelocity, Smoothing, modelEntity));
    }
}