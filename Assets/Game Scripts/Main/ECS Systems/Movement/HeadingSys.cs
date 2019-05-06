using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(MainGameGroup))]
//[UpdateAfter(typeof(ShipSpawnerSys))]
public class HeadingSys : JobComponentSystem
{
    /// <summary>
    /// Uses current pos and rot to set the target heading. Lerps current angular velocity and current heading towards target values in order to set a heading to reach target.
    /// </summary>
    [BurstCompile]
    private struct Job : IJobForEach<Translation, MoveDestination, Heading, AngularVelocity>
    {
        public float Dt;

        public void Execute([ReadOnly] ref Translation tran, [ReadOnly] ref MoveDestination dest, ref Heading heading, ref AngularVelocity aVel)
        {
            float current = heading.CurrentHeading;

            // target heading
            float2 relativeDir = math.normalize(dest.Value - tran.Value.xy);
            float target = Heading.FromFloat2(relativeDir);
            //float target = gmath.ToAngleRange360(math.degrees(math.atan2(relativeDir.x, relativeDir.y)));

            // we want to turn
            float signedInnerAngle = gmath.SignedInnerAngle(current, target);

            // lerp towards desired angular velocity
            // DO SOME MAGIC IN HERE

            // I need to cover d deg in t seconds. I don't want to overshoot so I should slow down when required

            // SHORTCUT: Set current velocity directly and instantly
            float sign = math.sign(signedInnerAngle);
            float currentAVel = math.min(aVel.MaxSpeed * Dt, math.abs(signedInnerAngle)) * sign;
            current = gmath.ToAngleRange360(current + currentAVel);

            // assign values
            aVel.CurrentVelocity = currentAVel;
            heading.CurrentHeading = current;
            heading.TargetHeading = target;

            //Logger.Log($"dest.Value: {dest.Value}, currentAVel: {currentAVel}, current: {current}, relativeDir: {relativeDir}, target: {target}, signedInnerAngle: {signedInnerAngle}");
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var job = new Job()
        {
            Dt = Time.deltaTime
        };

        JobHandle jh = job.ScheduleSingle(this, inputDeps);
        return jh;
    }
}