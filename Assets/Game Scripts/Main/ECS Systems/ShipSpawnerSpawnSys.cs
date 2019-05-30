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

    protected override void OnCreate()
    {
        cmdBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (Time.frameCount < 20f)
        {
            return inputDeps;
        }
        
        var job = new SpawnJob
        {
            CommandBuffer = cmdBufferSystem.CreateCommandBuffer(),
            Rand = new Random(Rand.New().NextUInt()),
            Time = Time.time,
            Dt = Time.deltaTime,
        };

        JobHandle jh = job.ScheduleSingle(this, inputDeps);
        cmdBufferSystem.AddJobHandleForProducer(jh);
        return jh;
    }

    //[BurstCompile]
    private struct SpawnJob : IJobForEachWithEntity<ShipSpawner, LocalToWorld, Translation, Rotation>
    {
        public EntityCommandBuffer CommandBuffer;
        public Random Rand;
        public float Time;
        public float Dt;

        public void Execute(Entity entity, int index, ref ShipSpawner spawner, [ReadOnly] ref LocalToWorld location, [ReadOnly] ref Translation tran, [ReadOnly] ref Rotation rot)
        {
            int maxToSpawn = spawner.MaxShips - spawner.ActiveShipCount;
            float desiredSpawnCount = spawner.SpawnRatePerSecond * Dt + spawner.SpawnCountRemainder;
            int flooredSpawnCount = (int)desiredSpawnCount;
            spawner.SpawnCountRemainder = desiredSpawnCount - flooredSpawnCount;
            int spawnCount = math.min(flooredSpawnCount, maxToSpawn);
            spawner.ActiveShipCount += spawnCount;

            float3 ss = spawner.SpawnSpread;
            float heading = Heading.FromQuaternion(rot.Value);
            float2 moveDest = tran.Value.xy + Heading.ToFloat2(heading);
            for (int i = 0; i < spawnCount; i++)
            {
                float3 pos = math.transform(location.Value, new float3(Rand.NextFloat(-ss.x, ss.x),
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