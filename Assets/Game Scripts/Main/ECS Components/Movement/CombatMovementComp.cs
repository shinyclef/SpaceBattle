using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public struct CombatMovement : IComponentData
{
    public ChoiceType CurrentChoice;
    public float ChoiceSelectedTime;
    public float LastEvalTime;
    public half NoiseSeed;

    public CombatMovement(float noiseSeed01)
    {
        CurrentChoice = default;
        ChoiceSelectedTime = default;
        LastEvalTime = float.MinValue;
        NoiseSeed = new half(math.remap(0f, 1f, half.MinValue, half.MaxValue, noiseSeed01));
    }
}

public class CombatMovementComp : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new CombatMovement());
    }
}