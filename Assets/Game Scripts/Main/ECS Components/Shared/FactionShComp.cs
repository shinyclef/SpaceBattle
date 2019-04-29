using System;
using Unity.Entities;
using UnityEngine;

[Serializable]
public struct Faction : ISharedComponentData
{
    public Factions Value;
}

[RequiresEntityConversion]
public class FactionShComp : MonoBehaviour, IConvertGameObjectToEntity
{
    public Factions Value;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var faction = new Faction
        {
            Value = Value
        };

        dstManager.AddSharedComponentData(entity, faction);
    }
}