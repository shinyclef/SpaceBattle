using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public struct CombatAi : IComponentData
{
    public ChoiceType ActiveChoice;
    public float ChoiceSelectedTime;
    public float LastEvalTime;
    public half NoiseSeed;

    public CombatAi(float noiseSeed01)
    {
        ActiveChoice = default;
        ChoiceSelectedTime = default;
        LastEvalTime = float.MinValue;
        NoiseSeed = new half(math.remap(0f, 1f, half.MinValue, half.MaxValue, noiseSeed01));
    }
}

public class CombatAiComp : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new CombatAi());
    }
}