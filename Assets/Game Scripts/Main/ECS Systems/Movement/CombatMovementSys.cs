using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

[UpdateInGroup(typeof(MainGameGroup))]
[UpdateAfter(typeof(CombatAiSys))]
public class CombatMovementSys : JobComponentSystem
{
    public bool EnableDebugRays;
    private NativeArray<Random> rngs;

    protected override void OnCreate()
    {
        rngs = new NativeArray<Random>(JobsUtility.MaxJobThreadCount, Allocator.Persistent);
    }

    protected override void OnDestroy()
    {
        if (rngs.IsCreated)
        {
            rngs.Dispose();
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        for (int i = 0; i < JobsUtility.MaxJobThreadCount; i++)
        {
            rngs[i] = Rand.New();
        }

        inputDeps = new Job()
        {
            Rngs = rngs,
            PhysicsVelocityComps = GetComponentDataFromEntity<PhysicsVelocity>(true),
            ThrustComps = GetComponentDataFromEntity<Thrust>(true),
            Dt = Time.deltaTime,
        }.Schedule(this, inputDeps);

        return inputDeps;
    }

    //[BurstCompile]
    private struct Job : IJobForEach<CombatTarget, LocalToWorld, PhysicsVelocity, Weapon, CombatAi, MoveDestination>
    {
        [NativeDisableContainerSafetyRestriction] public NativeArray<Random> Rngs;
        [ReadOnly] public ComponentDataFromEntity<PhysicsVelocity> PhysicsVelocityComps;
        [ReadOnly] public ComponentDataFromEntity<Thrust> ThrustComps;
        public float Dt;

        #pragma warning disable 0649
        [NativeSetThreadIndex] private int threadId;
        #pragma warning restore 0649

        //private int eId;

        public void Execute(
            [ReadOnly] ref CombatTarget target,
            [ReadOnly] ref LocalToWorld l2w,
            [ReadOnly] ref PhysicsVelocity vel,
            [ReadOnly] ref Weapon wep,
            [ReadOnly] ref CombatAi ai,
            ref MoveDestination dest)
        {
            if (target.Entity == Entity.Null)
            {
                dest.Value = l2w.Position.xy + l2w.Up.xy;
                return;
            }

            //eId = entity.Index;

            float2 targPos = target.Pos;
            Random rand = Rngs[threadId];

            switch (ai.ActiveChoice)
            {
                case ChoiceType.FlyTowardsEnemyMulti:
                    float2 targVel = PhysicsVelocityComps[target.Entity].Linear.xy;
                    float2 targAccel = ThrustComps[target.Entity].CurrentAcceleration;
                    float2 leadPos;

                    float distSq = math.distancesq(targPos, l2w.Position.xy);
                    if (distSq > wep.ProjectileRange * wep.ProjectileRange * 2f && false)
                    {
                        leadPos = TargetLeadHelper.GetTargetLeadHitPosIterativeRough(l2w.Position.xy, vel.Linear.xy, targPos, targVel, targAccel, wep.ProjectileSpeed);
                    }
                    else
                    {
                        leadPos = TargetLeadHelper.GetTargetLeadHitPosIterative(l2w.Position.xy, vel.Linear.xy, targPos, targVel, targAccel, wep.ProjectileSpeed);
                    }

                    float maxLeadDistance = gmath.Magnitude(targVel) * wep.ProjectileLifeTime; // TODO: get delta time stuff right?
                    float leadPosDist = math.distance(targPos, leadPos);
                    leadPos = targPos + math.normalizesafe(leadPos - targPos) * math.min(maxLeadDistance, leadPosDist);

                    dest.Value = leadPos;
                    break;

                case ChoiceType.FlyAwayFromEnemy:
                    if (ai.ActiveChoice != ChoiceType.FlyAwayFromEnemy)
                    {
                        // we start by choosing to go left or right
                        float2 offset = (rand.NextBool() ? l2w.Right.xy : -l2w.Right.xy) * 0.3f;
                        targPos += offset;
                        dest.Value = targPos;
                    }
                    else
                    {
                        // draw a line from target, through a point a few units in front of the ship, and on for a few units beyond that
                        float2 inFrontOfShip = l2w.Position.xy + l2w.Up.xy * 5;
                        float2 offset = math.normalize(inFrontOfShip - targPos) * 10;
                        float2 desiredDestination = inFrontOfShip + offset;

                        // we do this lerp so that the original left/right decision is respected
                        dest.Value = math.lerp(dest.Value, desiredDestination, Dt * 5);
                    }

                    break;

                default:
                    dest.Value = l2w.Position.xy + l2w.Up.xy;
                    break;
            }

            Rngs[threadId] = rand;
        }
    }
}