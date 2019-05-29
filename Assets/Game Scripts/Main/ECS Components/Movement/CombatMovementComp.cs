using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public struct CombatMovement : IComponentData
{
    public ChoiceType CurrentChoice;
    public float LastEvalTime;
    public half NoiseSeed;
    public half NoiseWaveLen;
}

public class CombatMovementComp : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new CombatMovement());
    }
}