using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public struct Weapon : IComponentData
{
    public Entity ProjectilePrefab;
    public float3 SpawnOffset;
    public float FireInterval;
    public float CooldownEnd;
}

[RequiresEntityConversion]
public class WeaponComp : MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
{
    public GameObject ProjectilePrefab;
    public float3 SpawnOffset;
    public float FireInterval;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var weapon = new Weapon
        {
            ProjectilePrefab = conversionSystem.GetPrimaryEntity(ProjectilePrefab),
            SpawnOffset = SpawnOffset,
            FireInterval = FireInterval,
            CooldownEnd = 0f,
        };

        dstManager.AddComponentData(entity, weapon);
    }

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        referencedPrefabs.Add(ProjectilePrefab);
    }
}