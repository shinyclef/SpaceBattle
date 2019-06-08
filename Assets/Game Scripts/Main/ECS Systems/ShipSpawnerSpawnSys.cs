using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

[UpdateInGroup(typeof(SpawnerGameGroup))]
public class ShipSpawnerSpawnSys : JobComponentSystem
{
    private BeginInitializationEntityCommandBufferSystem cmdBufferSystem;
    private NativeArray<int> activeShipCounts;
    private JobHandle jh;

    public NativeArray<int> ActiveShipCounts
    {
        get
        {
            jh.Complete();
            return activeShipCounts;
        }
    }

    protected override void OnCreate()
    {
        cmdBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        activeShipCounts = new NativeArray<int>(Enum.GetNames(typeof(Factions)).Length, Allocator.Persistent);
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (Time.frameCount < 20f)
        {
            return inputDeps;
        }
        
        inputDeps = new SpawnJob
        {
            CommandBuffer = cmdBufferSystem.CreateCommandBuffer(),
            ActiveShipCounts = activeShipCounts,
            Rand = new Random(Rand.New().NextUInt()),
            Time = Time.time,
            Dt = Time.deltaTime,
        }.ScheduleSingle(this, inputDeps);

        cmdBufferSystem.AddJobHandleForProducer(inputDeps);
        jh = inputDeps;
        return inputDeps;
    }

    //[BurstCompile]
    private struct SpawnJob : IJobForEachWithEntity<LocalToWorld, Translation, Rotation, Faction, ShipSpawner>
    {
        public EntityCommandBuffer CommandBuffer;
        [NativeDisableParallelForRestriction] public NativeArray<int> ActiveShipCounts;
        public Random Rand;
        public float Time;
        public float Dt;

        public void Execute(Entity entity, int index, 
            [ReadOnly] ref LocalToWorld l2w, 
            [ReadOnly] ref Translation tran, 
            [ReadOnly] ref Rotation rot,
            [ReadOnly] ref Faction faction,
            ref ShipSpawner spawner)
        {
            int maxToSpawn = math.max(0, spawner.MaxShips - spawner.ActiveShipCount);
            float desiredSpawnCount = math.max(0f, spawner.SpawnRatePerSecond * Dt + spawner.SpawnCountRemainder);
            int flooredSpawnCount = (int)desiredSpawnCount;
            spawner.SpawnCountRemainder = desiredSpawnCount - flooredSpawnCount;
            int spawnCount = math.min(flooredSpawnCount, maxToSpawn);
            spawner.ActiveShipCount += spawnCount;
            ActiveShipCounts[(int)faction.Value] = spawner.ActiveShipCount;

            float3 ss = spawner.SpawnSpread;
            float heading = Heading.FromQuaternion(rot.Value);
            float2 moveDest = tran.Value.xy + Heading.ToFloat2(heading);
            for (int i = 0; i < spawnCount; i++)
            {
                float3 pos = math.transform(l2w.Value, new float3(Rand.NextFloat(-ss.x, ss.x),
                                                                       Rand.NextFloat(-ss.y, ss.y),
                                                                       Rand.NextFloat(-ss.z, ss.z)));

                Entity ship = CommandBuffer.Instantiate(spawner.ShipPrefab);
                CommandBuffer.SetComponent(ship, new Translation { Value = pos });
                CommandBuffer.SetComponent(ship, new Heading(heading));
                CommandBuffer.SetComponent(ship, new MoveDestination(moveDest, false));
                CommandBuffer.SetComponent(ship, new SpawnTime(Time));
                CommandBuffer.SetComponent(ship, new CombatMovement(Rand.NextFloat()));
                CommandBuffer.AddSharedComponent(ship, new ShipSpawnerOwnerSsShC(entity.Index, entity.Version));
            }
        }
    }
}