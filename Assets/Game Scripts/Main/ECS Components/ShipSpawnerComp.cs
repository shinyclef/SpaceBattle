using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;
using Collider = Unity.Physics.Collider;

[Serializable]
public struct ShipSpawner : IComponentData
{
    public Entity ShipPrefab;
    public int MaxShips;
    public float SpawnRatePerSecond;
    public int ActiveShipCount;
    public float3 SpawnSpread;
    public float SpawnCountRemainder;
}

[RequiresEntityConversion]
public class ShipSpawnerComp : MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
{
    public GameObject ShipPrefab;
    public GameObject ProjectilePrefab;
    public int MaxShips = 2000;
    public float SpawnRatePerSecond = 200;
    public float3 SpawnSpread;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        Entity shipPrimaryEntity = conversionSystem.GetPrimaryEntity(ShipPrefab);
        int faction = (int)ShipPrefab.GetComponent<FactionComp>().Value;
        var shipSpawner = new ShipSpawner
        {
            ShipPrefab = shipPrimaryEntity,
            MaxShips = MaxShips,
            SpawnRatePerSecond = SpawnRatePerSecond,
            SpawnSpread = SpawnSpread,
            ActiveShipCount = 0,
            SpawnCountRemainder = 0
        };

        dstManager.AddSharedComponentData(shipPrimaryEntity, new ShipSpawnerOwnerSsShC(0, 0));
        dstManager.AddComponentData(entity, shipSpawner);
        Scheduler.InvokeAfterOneFrame(() =>
        {
            var mass = dstManager.GetComponentData<PhysicsMass>(shipPrimaryEntity);
            mass.InverseInertia[0] = 0;
            mass.InverseInertia[1] = 0;
            dstManager.SetComponentData(shipPrimaryEntity, mass);

            unsafe
            {
                Collider* col = dstManager.GetComponentData<PhysicsCollider>(shipPrimaryEntity).ColliderPtr;
                CollisionFilter filter = col->Filter;
                filter.GroupIndex = -faction;
                col->Filter = filter;
            }
        });
    }

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        referencedPrefabs.Add(ShipPrefab);
        referencedPrefabs.Add(ProjectilePrefab);
    }
}