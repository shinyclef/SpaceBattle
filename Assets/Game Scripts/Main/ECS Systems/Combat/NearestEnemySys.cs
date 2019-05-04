using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(GameGroupPostPhysics))]
public class NearestEnemySys : JobComponentSystem
{
    private const float MinUpdateInterval = 0.2f;
    private BuildPhysicsWorld buildPhysicsWorldSys;

    protected override void OnCreate()
    {
        buildPhysicsWorldSys = World.GetOrCreateSystem<BuildPhysicsWorld>();
    }

    [BurstCompile]
    private struct Job : IJobForEachWithEntity<Translation, Rotation, PhysicsCollider, NearestEnemy>
    {
        public float Time;
        [ReadOnly] public PhysicsWorld PhysicsWorld;
        [ReadOnly] public CollisionWorld CollisionWorld;

        public void Execute(Entity entity, int index, [ReadOnly] ref Translation tran, [ReadOnly] ref Rotation rot, [ReadOnly] ref PhysicsCollider col, ref NearestEnemy nearestEnemy)
        {
            if (Time - nearestEnemy.LastRefreshTime < MinUpdateInterval)
            {
                return;
            }

            unsafe
            {
                CollisionFilter filter = new CollisionFilter
                {
                    CategoryBits = 1u << (int)PhysicsLayer.RayCast,
                    MaskBits = 1u << (int)PhysicsLayer.Ships,
                    GroupIndex = col.ColliderPtr->Filter.GroupIndex
                };
                
                PointDistanceInput pointInput = new PointDistanceInput
                {
                    Position = tran.Value,
                    MaxDistance = nearestEnemy.QueryRange,
                    Filter = filter
                };

                DistanceHit hit;
                CollisionWorld.CalculateDistance(pointInput, out hit);

                Entity hitEntity = PhysicsWorld.Bodies[hit.RigidBodyIndex].Entity;
                if (hitEntity == entity)
                {
                    nearestEnemy.Entity = Entity.Null;
                }
                else
                {
                    nearestEnemy.Entity = PhysicsWorld.Bodies[hit.RigidBodyIndex].Entity;
                }

                nearestEnemy.LastRefreshTime = Time;
                //Logger.Log($"{entity} found {nearestEnemy.Entity}.");
            }
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var job = new Job()
        {
            Time = Time.time,
            PhysicsWorld = buildPhysicsWorldSys.PhysicsWorld,
            CollisionWorld = buildPhysicsWorldSys.PhysicsWorld.CollisionWorld
        };

        JobHandle jh = job.ScheduleSingle(this, inputDeps);
        return jh;
    }
}