using System;
using Unity.Entities;
using UnityEngine;

[Serializable]
public struct CombatMovement : IComponentData
{
    public ChoiceType LastChoice;
    public float LastChoiceTime;
    public float LastEvalTime;
    public float LastHeading;
}

public class CombatMovementComp : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new CombatMovement());
    }
}