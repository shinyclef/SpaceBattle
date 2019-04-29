using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

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
        dstManager.AddSharedComponentData(shipPrimaryEntity, new ShipSpawnerOwnerSsShC(0, 0));

        var shipSpawner = new ShipSpawner
        {
            ShipPrefab = shipPrimaryEntity,
            MaxShips = MaxShips,
            SpawnRatePerSecond = SpawnRatePerSecond,
            SpawnSpread = SpawnSpread,
            ActiveShipCount = 0,
            SpawnCountRemainder = 0
        };
        
        dstManager.AddComponentData(entity, shipSpawner);
    }

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        referencedPrefabs.Add(ShipPrefab);
        referencedPrefabs.Add(ProjectilePrefab);
    }
}