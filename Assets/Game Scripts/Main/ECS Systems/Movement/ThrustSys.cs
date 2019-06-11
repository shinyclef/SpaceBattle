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

    //[BurstCompile]
    private struct ThrustJob : IJobForEachWithEntity<LocalToWorld, MoveDestination, PhysicsDamping, PhysicsMass, PhysicsVelocity, Thrust>
    {
        public float Dt;

        public void Execute(Entity e, int index,
            [ReadOnly] ref LocalToWorld l2w, 
            [ReadOnly] ref MoveDestination dest, 
            [ReadOnly] ref PhysicsDamping damp,
            [ReadOnly] ref PhysicsMass mass,
            ref PhysicsVelocity vel, 
            ref Thrust thrust)
        {
            /* ------------- */
            /* -- Angular -- */
            /* ------------- */

            // calculate the desired thrust vector
            float currentH = gmath.Float2ToHeading(l2w.Up.xy);
            //Logger.LogIf(l2w.Up.x < 0f, $"l2w.Up.xy: {l2w.Up.xy}, currentH: {currentH}");
            float2 relativeDir = math.normalize(dest.Value - l2w.Position.xy);
            float targetH = Heading.FromFloat2(relativeDir);

            // rotate towards it by applying angular force
            // let's just apply an angular force in the appropriate direction until the angular damping will slow me into the right direciton
            // later, I may want to consider cases where the angular damping is very low, and apply reverse thrust to help
            float signedInnerAngle = gmath.SignedInnerAngle(currentH, targetH);
            float sign = -math.sign(signedInnerAngle);

            // each timestep, velocity is multiplied with (velocity * (1 - damping)) -> motionVelocity.AngularVelocity *= math.clamp(1.0f - motionData.AngularDamping * Timestep, 0.0f, 1.0f);
            // this applies to both linear and angular velocity

            // calculate the 'slow down distance'
            const float slowDownDist = 90f;

            // utility = Slope * pow((input - XShift), E) + YShift
            float absAngle = math.abs(signedInnerAngle);
            float normDistance = math.clamp(absAngle / slowDownDist, 0f, 1f);
            float slowDownMultiplier = -1 * math.pow(normDistance - 1f, 2f) + 1f;

            // get added force and apply to velocity
            float addedForce = thrust.AngularAcceleration * Dt * sign * slowDownMultiplier;
            vel.ApplyAngularImpulse(mass, addedForce);
            if (math.abs(vel.Angular.z) > thrust.AngularMaxSpeed)
            {
                float compensationForce = (math.abs(vel.Angular.z) - thrust.AngularMaxSpeed) * -math.sign(vel.Angular.z);
                vel.ApplyAngularImpulse(mass, compensationForce);
            }

            //Logger.LogIf(targetH < 180f, $"currentH: {currentH}, l2w.Up.xy: {l2w.Up.xy}, signedInnerAngle: {signedInnerAngle}, normDistance: {normDistance}, av {vel.Angular}");
            //Logger.LogIf(targetH < 180f, $"currentH: {currentH}, l2w.Up.xy: {l2w.Up.xy}, normDistance: {normDistance}, av {vel.Angular}");
            //if (targetH < 180f && l2w.Up.x < -0.9f)
            //{
            //    Logger.LogError("Why did LocalToWorld.Up and Rotation.Value just flip?? I need to know which way is up!! Is LocalToWorld.Up not the correct way??");
            //}


            //Logger.LogIf(e.Index == 1031, $"up: {l2w.Up}, upZ: {System.Math.Round(l2w.Up.z, 6)}, currentH: {System.Math.Round(currentH, 6)}");
            //Logger.LogIf(e.Index == 1031, $"aVel: {a}, currentH: {currentH}, targetH: {targetH}, ");
            //Logger.LogIf(e.Index == 1031, $"entity: " + e);


            /* ------------ */
            /* -- Linear -- */
            /* ------------ */

            // thrust if we are rotated nearly the right way. This modifies physics velocity.

            if (math.abs(signedInnerAngle) < 20f)
            {
                float2 v = vel.Linear.xy;
                v += gmath.HeadingToFloat2(currentH) * thrust.Acceleration * Dt;
                float2 speedLimitCorrection = math.max(0f, gmath.Magnitude(v) - thrust.MaxSpeed) * -math.normalize(v);
                v += speedLimitCorrection;
                vel.Linear = new float3(v, 0f);
            }

            // cancel z velocity
            //Logger.Log("Z-Pos: " + l2w.Position.z);

            // assign values
            //heading.CurrentHeading = currentH;
            //heading.TargetHeading = targetH;

            //Logger.Log($"dest.Value: {dest.Value}, currentAVel: {currentAVel}, current: {current}, relativeDir: {relativeDir}, target: {target}, signedInnerAngle: {signedInnerAngle}");
        }
    }
}