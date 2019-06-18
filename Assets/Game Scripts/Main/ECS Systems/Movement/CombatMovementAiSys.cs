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
[UpdateAfter(typeof(CombatTargetSys))]
public class CombatMovementAiSys : JobComponentSystem
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

        var job = new Job()
        {
            Rngs = rngs,
            UtilityScoreBufs = GetBufferFromEntity<UtilityScoreBuf>(false),
            PhysicsVelocityComps = GetComponentDataFromEntity<PhysicsVelocity>(true),
            ThrustComps = GetComponentDataFromEntity<Thrust>(true),
            Decisions = AiDataSys.NativeData.Decisions,
            Choices = AiDataSys.NativeData.Choices,
            Considerations = AiDataSys.NativeData.Considerations,
            RecordedScores = AiDataSys.NativeData.RecordedScores,
            RecordedDecision = AiInspector.RecordedDecision,
            Time = Time.time,
            DeltaTime = Time.deltaTime,
            RecordedEntity = SelectionSys.SelectedEntity
        };

        JobHandle jh = job.Schedule(this, inputDeps);
        return jh;
    }

    //[BurstCompile]
    private struct Job : IJobForEachWithEntity<CombatTarget, LocalToWorld, PhysicsVelocity, Weapon, MoveDestination, CombatMovement>
    {
        [NativeDisableContainerSafetyRestriction] public NativeArray<Random> Rngs;
        [NativeDisableParallelForRestriction] public BufferFromEntity<UtilityScoreBuf> UtilityScoreBufs;
        [ReadOnly] public ComponentDataFromEntity<PhysicsVelocity> PhysicsVelocityComps;
        [ReadOnly] public ComponentDataFromEntity<Thrust> ThrustComps;
        [ReadOnly] public NativeArray<Decision> Decisions;
        [ReadOnly] public NativeArray<Choice> Choices;
        [ReadOnly] public NativeArray<Consideration> Considerations;
        [NativeDisableParallelForRestriction] [WriteOnly] public NativeArray<float> RecordedScores;
        public DecisionType RecordedDecision;

        public float Time;
        public float DeltaTime;
        public Entity RecordedEntity;

        #pragma warning disable 0649
        [NativeSetThreadIndex] private int threadId;
        #pragma warning restore 0649
        [NativeDisableParallelForRestriction] private DynamicBuffer<UtilityScoreBuf> utilityScores;

        //private int eId;

        public void Execute(Entity entity, int index,
            [ReadOnly] ref CombatTarget target,
            [ReadOnly] ref LocalToWorld l2w,
            [ReadOnly] ref PhysicsVelocity vel,
            [ReadOnly] ref Weapon wep,
            ref MoveDestination dest,
            ref CombatMovement cm)
        {
            //eId = entity.Index;
            if (target.Entity != Entity.Null)
            {
                //eId = entity.Index;
                float2 targPos = target.Pos;
                Random rand = Rngs[threadId];
                ChoiceType selectedChoice;
                if (Time - cm.LastEvalTime < 0.3f)
                {
                    selectedChoice = cm.CurrentChoice;
                }
                else
                {
                    cm.LastEvalTime = Time;

                    float distance = 0f;
                    bool hasDistance = false;
                    float angle = 0f;
                    bool hasAngle = false;

                    // make decision
                    utilityScores = UtilityScoreBufs[entity];
                    DecisionMaker dm = new DecisionMaker(ref Decisions, ref Choices, ref Considerations, ref utilityScores, Time,
                        cm.CurrentChoice, ref RecordedScores, RecordedDecision, entity == RecordedEntity);
                    bool hasNext = dm.PrepareDecision(DecisionType.CombatMovement, ref rand);
                    while (hasNext)
                    {
                        FactType requiredFact = dm.NextRequiredFactType;
                        // Logger.LogIf(entity.Index == 9, $"Required Fact: {requiredFact}");
                        float factValue;
                        switch (requiredFact)
                        {
                            case FactType.DistanceFromTarget:
                                if (!hasDistance)
                                {
                                    hasDistance = true;
                                    distance = math.distance(l2w.Position.xy, targPos);
                                }

                                factValue = distance;
                                break;

                            case FactType.AngleFromTarget:
                                if (!hasAngle)
                                {
                                    hasAngle = true;
                                    float2 dirToEnemy = math.normalizesafe(targPos - l2w.Position.xy);
                                    angle = gmath.AngleBetweenVectors(dirToEnemy, l2w.Up.xy);
                                }

                                factValue = angle;
                                break;

                            case FactType.Noise:
                                factValue = cm.NoiseSeed;
                                break;

                            case FactType.TimeSinceLastChoiceSelection:
                                factValue = Time - cm.ChoiceSelectedTime;
                                break;

                            case FactType.TimeSinceThisChoiceSelection:
                                factValue = dm.CurrentlyEvaluatedChoice == cm.CurrentChoice ? Time - cm.ChoiceSelectedTime : 0f;
                                break;

                            default:
                                //Logger.LogIf(entity.Index == 9, $"I'm unable to provide FactType: {requiredFact}");
                                factValue = 0f;
                                break;
                        }

                        hasNext = dm.EvaluateNextConsideration(factValue, ref rand);
                    }

                    selectedChoice = dm.SelectedChoice;
                    //Logger.LogIf(entity.Index == 9, $"selectedChoice was: {selectedChoice}");
                    utilityScores.Clear();
                }

                switch (selectedChoice)
                {
                    case ChoiceType.FlyTowardsEnemy:

                        // target pos + velocity

                        float2 targVel = PhysicsVelocityComps[target.Entity].Linear.xy;
                        float2 targAccel = ThrustComps[target.Entity].CurrentAcceleration;
                        float2 leadPos;

                        float distSq = math.distancesq(targPos, l2w.Position.xy);
                        if (distSq > wep.projectileRange * wep.projectileRange * 1.5f)
                        {
                            leadPos = targPos + targVel * wep.projectileLifeTime;
                        }
                        else
                        {
                            leadPos = TargetLeadHelper.GetTargetLeadHitPosIterative(l2w.Position.xy, vel.Linear.xy, targPos, targVel, targAccel, wep.projectileSpeed);
                        }

                        float dot01 = math.clamp(math.dot(vel.Linear.xy, targVel) * 8f - 8f, 0f, 1f); // I only want the top small percentage of this range, cause we usually want to lead
                        leadPos = math.lerp(leadPos, targPos, dot01); // this is so we stay fixed when behind them without wobbling around

                        float maxLeadDistance = gmath.Magnitude(targVel) * wep.projectileLifeTime * 0.9f; // TODO: get delta time stuff right?
                        float leadPosDist = math.distance(leadPos, l2w.Position.xy);
                        leadPos = targPos + math.normalizesafe(leadPos - targPos) * math.min(maxLeadDistance, leadPosDist);

                        dest.Value = leadPos;
                        dest.IsCombatTarget = true;
                        break;

                    case ChoiceType.FlyAwayFromEnemy:
                        if (cm.CurrentChoice != ChoiceType.FlyAwayFromEnemy)
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
                            dest.Value = math.lerp(dest.Value, desiredDestination, DeltaTime * 5);
                        }

                        dest.IsCombatTarget = false;
                        break;

                    default:
                        dest.Value = l2w.Position.xy + l2w.Up.xy;
                        dest.IsCombatTarget = false;
                        Rngs[threadId] = rand;
                        return;
                }

                dest.IsCombatTarget = true;
                if (cm.CurrentChoice != selectedChoice)
                {
                    //Logger.LogIf(entity.Index == 9, $"New Choice was: {selectedChoice}");
                    cm.CurrentChoice = selectedChoice;
                    cm.ChoiceSelectedTime = Time;
                }

                Rngs[threadId] = rand;
            }
            else
            {
                dest.Value = l2w.Position.xy + l2w.Up.xy;
                dest.IsCombatTarget = false;
            }
        }
    }
}