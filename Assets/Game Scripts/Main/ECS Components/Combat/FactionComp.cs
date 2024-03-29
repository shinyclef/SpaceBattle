﻿using System;
using Unity.Entities;
using UnityEngine;

[Serializable]
public struct Faction : IComponentData
{
    public Factions Value;
}

[RequiresEntityConversion]
public class FactionComp : MonoBehaviour, IConvertGameObjectToEntity
{
    public Factions Value;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var faction = new Faction
        {
            Value = Value
        };

        dstManager.AddComponentData(entity, faction);
    }
}