using System;
using Unity.Entities;

[Serializable]
public struct Wingmate : IComponentData
{
    public Entity SquadLead;
    public byte FormationPos;
}