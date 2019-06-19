using System;
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
    public float ProjectileLifeTime;
    public float ProjectileSpeed;
    public half FireArcDegreesFromCenter;
    public int LastBurstShot;
    public float BurstShotCooldownEnd;

    public float ProjectileRange { get { return ProjectileLifeTime * ProjectileSpeed; } }
}

[RequiresEntityConversion]
public class WeaponComp : MonoBehaviour, IConvertGameObjectToEntity
{
    public GameObject ProjectilePrefab;
    public float3 SpawnOffset;
    public float FireMajorInterval;
    public float FireMinorInterval;
    public int FireBurstCount;
    public float FireArcDegreesFromCenter;

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
            ProjectileLifeTime = lifeTime,
            ProjectileSpeed = speed,
            FireArcDegreesFromCenter = new half(FireArcDegreesFromCenter),
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
}