using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(PhysicsGameGroup))]
[DisableAutoCreation]
public class NearestEnemySysOld : JobComponentSystem
{
    private const float MinUpdateInterval = 0.0f; // TODO: return nearest enemy search interval to 0.5f
    private BuildPhysicsWorld buildPhysicsWorldSys;

    protected override void OnCreate()
    {
        buildPhysicsWorldSys = World.GetOrCreateSystem<BuildPhysicsWorld>();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var job = new NearestCastJob()
        {
            Time = Time.time,
            CollisionWorld = buildPhysicsWorldSys.PhysicsWorld.CollisionWorld
        };

        JobHandle jh = job.Schedule(this, inputDeps);
        return jh;
    }

    [BurstCompile]
    private struct NearestCastJob : IJobForEachWithEntity<Translation, PhysicsCollider, NearestEnemy>
    {
        public float Time;
        [ReadOnly] public CollisionWorld CollisionWorld;

        public void Execute(Entity entity, int index, [ReadOnly] ref Translation tran, [ReadOnly] ref PhysicsCollider col, ref NearestEnemy nearestEnemy)
        {
            if (Time - nearestEnemy.LastRefreshTime < MinUpdateInterval || CollisionWorld.Bodies.Length == 0)
            {
                return;
            }

            unsafe
            {
                CollisionFilter filter = new CollisionFilter
                {
                    BelongsTo = 1u << (int)PhysicsLayer.RayCast,
                    CollidesWith = 1u << (int)PhysicsLayer.Ships,
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

                Entity hitEntity = CollisionWorld.Bodies[hit.RigidBodyIndex].Entity;
                if (hitEntity == entity ||
                    CollisionWorld.Bodies[hit.RigidBodyIndex].Collider->Filter.GroupIndex == col.ColliderPtr->Filter.GroupIndex) // TODO: Remove this temporary case when collider groups are working
                {
                    nearestEnemy.Entity = Entity.Null;
                }
                else
                {
                    nearestEnemy.Entity = CollisionWorld.Bodies[hit.RigidBodyIndex].Entity;
                }

                nearestEnemy.LastRefreshTime = Time;
                //Logger.Log($"{entity} found {nearestEnemy.Entity}.");
            }
        }
    }
}