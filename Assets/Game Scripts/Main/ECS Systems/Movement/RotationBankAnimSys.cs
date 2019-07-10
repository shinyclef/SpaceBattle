using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(MainGameGroup))]
[UpdateAfter(typeof(ThrustSys))]
public class RotationBankAnimSys : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        inputDeps = new Job()
        {
            Dt = Time.fixedDeltaTime, // TODO: Confirm time
            Rotations = GetComponentDataFromEntity<Rotation>(),
        }.Schedule(this, inputDeps);

        return inputDeps;
    }

    [BurstCompile]
    private struct Job : IJobForEachWithEntity<PhysicsVelocity, RotationBankAnim>
    {
        public float Dt;
        [NativeDisableParallelForRestriction] public ComponentDataFromEntity<Rotation> Rotations;

        public void Execute(Entity entity, int index, [ReadOnly] ref PhysicsVelocity vel, ref RotationBankAnim rotBankAnim)
        {
            Rotation rot = Rotations[rotBankAnim.ModelEntity];
            float maxBankVel = rotBankAnim.MaxBankAngularVelocity;
            float maxBankDeg = rotBankAnim.MaxBankDegrees;
            float targetRotDeg = math.remap(-maxBankVel, maxBankVel, -maxBankDeg, maxBankDeg, math.degrees(vel.Angular.z));
            rotBankAnim.CurrentRot = math.lerp(rotBankAnim.CurrentRot, targetRotDeg, Dt * rotBankAnim.Smoothing);
            rot.Value = quaternion.EulerXYZ(0f, math.radians(rotBankAnim.CurrentRot), 0f);
            Rotations[rotBankAnim.ModelEntity] = rot;
        }
    }
}