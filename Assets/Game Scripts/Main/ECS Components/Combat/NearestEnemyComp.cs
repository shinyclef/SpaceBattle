using System;
using Unity.Entities;
using UnityEngine;

[Serializable]
public struct NearestEnemy : IComponentData
{
    public Entity Entity;
    public bool UpdatePending;
    public float LastUpdatedTime;
}

public class NearestEnemyComp : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new NearestEnemy());
    }
}