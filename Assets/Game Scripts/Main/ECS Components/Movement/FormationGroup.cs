using System;
using System.Runtime.CompilerServices;
using Unity.Entities;

[Serializable]
public struct FormationGroup : IComponentData
{
    public int Mask;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetFormationPos(byte pos)
    {
        pos--;
        Mask |= (1 << pos);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UnsetFormationPos(byte pos)
    {
        pos--;
        Mask &= ~(1 << pos);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool FormationPosIsSet(byte pos)
    {
        pos--;
        return (Mask & (1 << pos)) == (1 << pos);
    }

    public byte OccupyFreeSlot()
    {
        for (byte i = 1; i <= 32; i++)
        {
            if (!FormationPosIsSet(i))
            {
                SetFormationPos(i);
                return i;
            }
        }

        return 0;
    }
}