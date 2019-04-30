using System;
using Unity.Entities;
using UnityEngine;

[Serializable]
public struct NearestEnemy : IComponentData
{
    public Entity Entity;
    public float QueryRange;
}

public class NearestEnemyComp : MonoBehaviour, IConvertGameObjectToEntity
{
    public float QueryRange = 50f;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new NearestEnemy() { QueryRange = QueryRange });
    }
}