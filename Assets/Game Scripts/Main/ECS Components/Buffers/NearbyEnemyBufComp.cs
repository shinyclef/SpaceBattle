using Unity.Entities;
using UnityEngine;

[InternalBufferCapacity(5)]
public struct NearbyEnemyBuf : IBufferElementData
{
    public static implicit operator Entity(NearbyEnemyBuf e) { return e.Enemy; }
    public static implicit operator NearbyEnemyBuf(Entity e) { return new NearbyEnemyBuf { Enemy = e }; }

    public Entity Enemy;
}

public class NearbyEnemyBufComp : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddBuffer<NearbyEnemyBuf>(entity);
    }
}