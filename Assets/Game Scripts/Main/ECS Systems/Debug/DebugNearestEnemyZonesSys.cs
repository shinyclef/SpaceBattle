using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(MainGameGroup))]
public class DebugNearestEnemyZonesSys : JobComponentSystem
{
    public static DebugNearestEnemyZonesSys I;
    private NearestEnemyRequestSys sys;
    private List<Color> cols;

    protected override void OnCreate()
    {
        I = this;
        sys = World.GetOrCreateSystem<NearestEnemyRequestSys>();
        cols = new List<Color>() { Color.green, Color.red, Color.blue };
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        sys.FinalJobHandle.Complete();

        NativeHashMap<int3, Entity> bufEntities = sys.NearestEnemiesBuffers;
        NativeArray<int3> keys = bufEntities.GetKeyArray(Allocator.Temp);
        NativeArray<Entity> vals = bufEntities.GetValueArray(Allocator.Temp);
        BufferFromEntity<NearbyEnemyBuf> bufs = GetBufferFromEntity<NearbyEnemyBuf>(true);
        ComponentDataFromEntity<LocalToWorld> l2ws = GetComponentDataFromEntity<LocalToWorld>(true);

        for (int i = 0; i < keys.Length; i++)
        {
            Entity e = bufs[vals[i]][0];
            Debug.DrawLine(new float3(keys[i].xy, 0), l2ws[e].Position, cols[keys[i].z - 1], 0.01f);
        }

        return inputDeps;
    }
}