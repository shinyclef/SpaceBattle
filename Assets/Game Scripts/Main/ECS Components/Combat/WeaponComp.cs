using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;
using Collider = Unity.Physics.Collider;

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
        Entity projPrimaryEntity = conversionSystem.GetPrimaryEntity(ProjectilePrefab);
        int faction = (int)ProjectilePrefab.GetComponent<FactionComp>().Value;
        var weapon = new Weapon
        {
            ProjectilePrefab = conversionSystem.GetPrimaryEntity(ProjectilePrefab),
            SpawnOffset = SpawnOffset,
            FireInterval = FireInterval,
            CooldownEnd = 0f,
        };

        dstManager.AddComponentData(entity, weapon);
        Scheduler.InvokeAfterOneFrame(() =>
        {
            unsafe
            {
                Collider* col = dstManager.GetComponentData<PhysicsCollider>(projPrimaryEntity).ColliderPtr;
                CollisionFilter filter = col->Filter;
                filter.GroupIndex = -faction;
                col->Filter = filter;
            }
        });
    }

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        referencedPrefabs.Add(ProjectilePrefab);
    }
}