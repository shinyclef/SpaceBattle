using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
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
        rngs = new NativeArray<Random>(JobsUtility.MaxJobThreadCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
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
            UtilityScoreBufs = GetBufferFromEntity<UtilityScoreBuf>(),
            Decisions = AiDataSys.NativeData.Decisions,
            Choices = AiDataSys.NativeData.Choices,
            Considerations = AiDataSys.NativeData.Considerations,
            RecordedScores = AiDataSys.NativeData.RecordedScores,
            RecordedDecision = AiInspector.RecordedDecision,
            Time = Time.time,
            DeltaTime = Time.deltaTime
        };

        JobHandle jh = job.Schedule(this, inputDeps);
        return jh;
    }

    //TODO [BurstCompile]
    private struct Job : IJobForEachWithEntity<CombatTarget, LocalToWorld, MoveDestination, CombatMovement, Heading>
    {
        [NativeDisableContainerSafetyRestriction] public NativeArray<Random> Rngs;
        [NativeDisableParallelForRestriction] public BufferFromEntity<UtilityScoreBuf> UtilityScoreBufs;
        [ReadOnly] public NativeArray<Decision> Decisions;
        [ReadOnly] public NativeArray<Choice> Choices;
        [ReadOnly] public NativeArray<Consideration> Considerations;
        [NativeDisableParallelForRestriction] [WriteOnly] public NativeArray<float> RecordedScores;
        public DecisionType RecordedDecision;

        public float Time;
        public float DeltaTime;

    #pragma warning disable 0649
        [NativeSetThreadIndex] private int threadId;
        #pragma warning restore 0649
        [NativeDisableParallelForRestriction] private DynamicBuffer<UtilityScoreBuf> utilityScores;

        private int eId;

        public void Execute(Entity entity, int index,
            [ReadOnly] ref CombatTarget target,
            [ReadOnly] ref LocalToWorld l2w,
            ref MoveDestination dest,
            ref CombatMovement cm,
            ref Heading heading)
        {
            eId = entity.Index;
            if (target.Entity != Entity.Null)
            {
                //eId = entity.Index;
                float2 targetPos = target.Pos;
                Random rand = Rngs[threadId];
                ChoiceType selectedChoice;
                if (Time - cm.LastEvalTime < 0.0f)
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
                        cm.CurrentChoice, ref RecordedScores, RecordedDecision, entity.Index == 8);
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
                                    distance = math.distance(l2w.Position.xy, targetPos);
                                }

                                factValue = distance;
                                break;

                            case FactType.AngleFromTarget:
                                if (!hasAngle)
                                {
                                    hasAngle = true;
                                    float headingToEnemy = Heading.FromFloat2(math.normalize(targetPos - l2w.Position.xy));
                                    angle = math.abs(gmath.SignedInnerAngle(heading.CurrentHeading, headingToEnemy));
                                }

                                factValue = angle;
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
                        dest.Value = targetPos;
                        dest.IsCombatTarget = true;
                        break;

                    case ChoiceType.FlyAwayFromEnemy:
                        if (cm.CurrentChoice != ChoiceType.FlyAwayFromEnemy)
                        {
                            // we start by choosing to go left or right
                            float2 offset = (rand.NextBool() ? l2w.Right.xy : -l2w.Right.xy) * 0.3f;
                            targetPos += offset;
                            
                            dest.Value = targetPos;
                            
                            //cm.LastHeading = Heading.FromFloat2(targetPos - l2w.Position.xy);
                            //Logger.LogIf(entity.Index == 9, "Head: " + Heading.ToFloat2(cm.LastHeading));
                        }
                        else
                        {
                            // draw a line from target, through a point a few units in front of the ship, and on for a few units beyond that
                            float2 inFrontOfShip = l2w.Position.xy + l2w.Up.xy * 5;
                            float2 offset = math.normalize(inFrontOfShip - targetPos) * 10;
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