using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

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
    private struct ThrustJob : IJobForEachWithEntity<LocalToWorld, MoveDestination, PhysicsMass, PhysicsVelocity, Heading, Thrust>
    {
        public float Dt;

        public void Execute(Entity e, int index,
            [ReadOnly] ref LocalToWorld l2w, 
            [ReadOnly] ref MoveDestination dest, 
            [ReadOnly] ref PhysicsMass mass, 
            ref PhysicsVelocity vel, 
            ref Heading heading, 
            ref Thrust thrust)
        {
            // update heading
            ///heading.CurrentHeading = l2w.Up.z;


            /* ------------- */
            /* -- Angular -- */
            /* ------------- */

            // calculate the desired thrust vector
            float currentH = gmath.Float2ToHeading(l2w.Up.xy);
            Logger.LogIf(l2w.Up.x < 0f, $"l2w.Up.xy: {l2w.Up.xy}, currentH: {currentH}");
            float2 relativeDir = math.normalize(dest.Value - l2w.Position.xy);
            float targetH = Heading.FromFloat2(relativeDir);

            // rotate towards it by applying angular force
            // let's just apply an angular force in the appropriate direction until the angular damping will slow me into the right direciton
            // later, I may want to consider cases where the angular damping is very low, and apply reverse thrust to help
            float signedInnerAngle = gmath.SignedInnerAngle(currentH, targetH);
            float sign = math.sign(signedInnerAngle);

            float3 a = vel.Angular;
            a.z = math.clamp(a.z + thrust.AngularAcceleration * Dt * sign, -thrust.AngularMaxSpeed, thrust.AngularMaxSpeed);
            vel.Angular = -a;

            //Logger.LogIf(e.Index == 1031, $"up: {l2w.Up}, upZ: {System.Math.Round(l2w.Up.z, 6)}, currentH: {System.Math.Round(currentH, 6)}");
            //Logger.LogIf(e.Index == 1031, $"aVel: {a}, currentH: {currentH}, targetH: {targetH}, ");
            //Logger.LogIf(e.Index == 1031, $"entity: " + e);


            /* ------------ */
            /* -- Linear -- */
            /* ------------ */

            // thrust if we are rotated nearly the right way. This modifies physics velocity.
            
            if (math.abs(signedInnerAngle) < 5f)
            {
                float2 v = vel.Linear.xy;
                v += gmath.HeadingToFloat2(currentH) * thrust.Acceleration * Dt;
                v += vel.Linear.xy;
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