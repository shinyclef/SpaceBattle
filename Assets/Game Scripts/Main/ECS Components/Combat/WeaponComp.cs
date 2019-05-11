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
    public float FireMajorInterval;
    public float FireMinorInterval;
    public int FireBurstCount;
    public float CooldownEnd;
    public float projectileLifeTime;
    public float projectileSpeed;
    public int LastBurstShot;
    public float BurstShotCooldownEnd;

    public float projectileRange { get { return projectileLifeTime * projectileSpeed; } }
}

[RequiresEntityConversion]
public class WeaponComp : MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
{
    public GameObject ProjectilePrefab;
    public float3 SpawnOffset;
    public float FireMajorInterval;
    public float FireMinorInterval;
    public int FireBurstCount;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        Entity projPrimaryEntity = conversionSystem.GetPrimaryEntity(ProjectilePrefab);
        int faction = (int)ProjectilePrefab.GetComponent<FactionComp>().Value;
        float lifeTime = ProjectilePrefab.GetComponent<LifeTimeComp>().Value;
        float speed = ProjectilePrefab.GetComponent<VelocityComp>().Speed;

        var weapon = new Weapon
        {
            ProjectilePrefab = conversionSystem.GetPrimaryEntity(ProjectilePrefab),
            SpawnOffset = SpawnOffset,
            FireMajorInterval = FireMajorInterval,
            FireMinorInterval = FireMinorInterval,
            FireBurstCount = FireBurstCount,
            CooldownEnd = 0f,
            projectileLifeTime = lifeTime,
            projectileSpeed = speed
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