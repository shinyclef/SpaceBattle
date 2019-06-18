using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using Unity.Physics.Extensions;

[AlwaysUpdateSystem]
[UpdateInGroup(typeof(MainGameGroup))]
public class ThrustSys : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        inputDeps = new ThrustJob()
        {
            Dt = Time.deltaTime,
        }.Schedule(this, inputDeps);

        return inputDeps;
    }

    [BurstCompile]
    private struct ThrustJob : IJobForEachWithEntity<LocalToWorld, MoveDestination, PhysicsMass, PhysicsVelocity, Thrust>
    {
        public float Dt;

        public void Execute(Entity e, int index,
            [ReadOnly] ref LocalToWorld l2w, 
            [ReadOnly] ref MoveDestination dest, 
            [ReadOnly] ref PhysicsMass mass,
            ref PhysicsVelocity vel, 
            ref Thrust thrust)
        {
            /* ------------- */
            /* -- Angular -- */
            /* ------------- */

            const float MaxCorrectionAngle = 10f;

            // calculate the desired thrust vector
            float currentH = gmath.Float2ToHeading(l2w.Up.xy);
            float2 targetDir = math.normalizesafe(dest.Value - l2w.Position.xy);
            float degDelta = math.clamp(gmath.AngleBetweenVectorsSigned(math.normalizesafe(vel.Linear.xy), targetDir), -MaxCorrectionAngle, MaxCorrectionAngle);
            targetDir = gmath.RotateVector(targetDir, degDelta);
            float targetH = gmath.Float2ToHeading(targetDir);

            // rotate towards it by applying angular force
            float signedInnerAngle = gmath.SignedInnerAngle(currentH, targetH);
            float sign = -math.sign(signedInnerAngle);

            // calculate the 'slow down distance'
            const float slowDownDist = 90f;
            float absAngle = math.abs(signedInnerAngle);
            float normDistance = math.clamp(absAngle / slowDownDist, 0f, 1f);
            float slowDownMultiplier = -1 * (normDistance - 1f) * (normDistance - 1f) + 1f; // utility = Slope * pow((input - XShift), E) + YShift

            // get added force and apply to velocity
            float addedAForce = thrust.AngularAcceleration * Dt * sign * slowDownMultiplier;
            vel.ApplyAngularImpulse(mass, addedAForce);
            if (math.abs(vel.Angular.z) > thrust.AngularMaxSpeed)
            {
                float compensationForce = (math.abs(vel.Angular.z) - thrust.AngularMaxSpeed) * -math.sign(vel.Angular.z);
                math.select(0f, compensationForce, math.abs(vel.Angular.z) > thrust.AngularMaxSpeed);
                vel.ApplyAngularImpulse(mass, compensationForce);
            }

            /* ------------ */
            /* -- Linear -- */
            /* ------------ */

            // thrust if we are rotated nearly the right way. This modifies physics velocity
            float2 addedForce = gmath.HeadingToFloat2(currentH) * thrust.Acceleration * Dt;// * (1 - (slowDownMultiplier / 5f));
            vel.ApplyLinearImpulse(mass, new float3 (addedForce, 0f));
            float2 speedLimitCorrection = math.max(0f, gmath.Magnitude(vel.Linear.xy) - thrust.MaxSpeed) * -math.normalize(vel.Linear.xy);
            vel.ApplyLinearImpulse(mass, new float3(speedLimitCorrection, 0f));
            thrust.CurrentAcceleration = addedForce + speedLimitCorrection;
        }
    }
}