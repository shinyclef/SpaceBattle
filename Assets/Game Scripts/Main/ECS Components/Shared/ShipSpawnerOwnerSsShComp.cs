using System;
using Unity.Entities;

[Serializable]
public struct ShipSpawnerOwnerSsShC : ISystemStateSharedComponentData
{
    public int EntityIndex;
    public int EntityVer;

    public ShipSpawnerOwnerSsShC(int entityIndex, int entityVer)
    {
        EntityIndex = entityIndex;
        EntityVer = entityVer;
    }
}