using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

[UpdateInGroup(typeof(GameGroupPostPhysics))]
public class NearestEnemySys : JobComponentSystem
{
    private BuildPhysicsWorld buildPhysicsWorldSys;

    protected override void OnCreate()
    {
        buildPhysicsWorldSys = World.GetOrCreateSystem<BuildPhysicsWorld>();
    }

    [BurstCompile]
    private struct Job : IJobForEachWithEntity<Translation, Rotation, PhysicsCollider, NearestEnemy>
    {
        [ReadOnly] public PhysicsWorld PhysicsWorld;

        public void Execute(Entity entity, int index, [ReadOnly] ref Translation tran, [ReadOnly] ref Rotation rot, [ReadOnly] ref PhysicsCollider col, ref NearestEnemy nearestEnemy)
        {
            unsafe
            {
                ColliderDistanceInput input = new ColliderDistanceInput
                {
                    Collider = col.ColliderPtr,
                    Transform = new RigidTransform(rot.Value, tran.Value),
                    MaxDistance = nearestEnemy.QueryRange
                };

                DistanceHit hit;
                PhysicsWorld.CalculateDistance(input, out hit);
                nearestEnemy.Entity = PhysicsWorld.Bodies[hit.RigidBodyIndex].Entity;
            }
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var job = new Job()
        {
            PhysicsWorld = buildPhysicsWorldSys.PhysicsWorld,
        };

        JobHandle jh = job.Schedule(this, inputDeps);
        return jh;
    }
}