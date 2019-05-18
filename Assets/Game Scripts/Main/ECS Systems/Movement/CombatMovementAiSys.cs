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
            TranslationComps = GetComponentDataFromEntity<Translation>(),
            Decisions = AiLoadSys.Data.Decisions,
            Choices = AiLoadSys.Data.Choices,
            ConsiderationIndecies = AiLoadSys.Data.ConsiderationIndecies,
            Considerations = AiLoadSys.Data.Considerations,
            Time = Time.time
        };

        JobHandle jh = job.Schedule(this, inputDeps);
        return jh;
    }

    [BurstCompile]
    private struct Job : IJobForEachWithEntity<CombatTarget, LocalToWorld, MoveDestination, CombatMovement, Heading>
    {
        [NativeDisableContainerSafetyRestriction] public NativeArray<Random> Rngs;
        [NativeDisableParallelForRestriction] public BufferFromEntity<UtilityScoreBuf> UtilityScoreBufs;
        [ReadOnly] public ComponentDataFromEntity<Translation> TranslationComps;
        [ReadOnly] public NativeArray<Decision> Decisions;
        [ReadOnly] public NativeArray<Choice> Choices;
        [ReadOnly] public NativeArray<ushort> ConsiderationIndecies;
        [ReadOnly] public NativeArray<Consideration> Considerations;

        public float Time;

        #pragma warning disable 0649
        [NativeSetThreadIndex] private int threadId;
        #pragma warning restore 0649
        [NativeDisableParallelForRestriction] private DynamicBuffer<UtilityScoreBuf> utilityScores;
        private float2 targetPos;

        private int eId;

        public void Execute(Entity entity, int index,
            [ReadOnly] ref CombatTarget enemy,
            [ReadOnly] ref LocalToWorld l2w,
            ref MoveDestination dest,
            ref CombatMovement cm,
            ref Heading heading)
        {
            if (enemy.Value != Entity.Null && TranslationComps.Exists(enemy.Value))
            {
                eId = entity.Index;
                targetPos = TranslationComps[enemy.Value].Value.xy;
                

                Random rand = Rngs[threadId];
                ChoiceType selectedChoice;
                if (Time - cm.LastChoiceTime < 0.1f)
                {
                    selectedChoice = cm.LastChoice;
                }
                else
                {
                    // make decision
                    utilityScores = UtilityScoreBufs[entity];

                    DecisionMaker dm = new DecisionMaker(Decisions, Choices, ConsiderationIndecies, Considerations, utilityScores);
                    dm.PrepareDecision(DecisionType.CombatMovement, ref rand);
                    bool hasNext;
                    do
                    {
                        FactType requiredFact = dm.NextRequiredFactType;
                        // Logger.LogIf(entity.Index == 9, $"Required Fact: {requiredFact}");
                        float factValue;
                        switch (requiredFact)
                        {
                            case FactType.DistanceFromTarget:
                                factValue = math.distance(l2w.Position.xy, targetPos);
                                break;

                            case FactType.AngleFromTarget:
                                factValue = math.abs(gmath.SignedInnerAngle(heading.CurrentHeading, heading.TargetHeading));
                                break;

                            default:
                                //Logger.LogIf(entity.Index == 9, $"I'm unable to provide FactType: {requiredFact}");
                                factValue = 0f;
                                break;
                        }

                        hasNext = dm.EvaluateNextConsideration(factValue);
                    }
                    while (hasNext);

                    selectedChoice = dm.SelectedChoice;
                    //Logger.LogIf(entity.Index == 9, $"selectedChoice was: {selectedChoice}");
                    utilityScores.Clear();
                }

                switch (selectedChoice)
                {
                    case ChoiceType.FlyTowardEnemy:
                        dest.Value = targetPos;
                        dest.IsCombatTarget = true;
                        break;

                    case ChoiceType.FlyAwayFromEnemy:
                        if (cm.LastChoice != ChoiceType.FlyAwayFromEnemy)
                        {
                            float2 offset = rand.NextBool() ? l2w.Right.xy : -l2w.Right.xy;
                            targetPos += offset;
                            dest.Value = targetPos;
                            cm.LastHeading = Heading.FromFloat2(targetPos - l2w.Position.xy);
                            //Logger.LogIf(entity.Index == 9, "Head: " + Heading.ToFloat2(cm.LastHeading));
                        }
                        else
                        {
                            float2 dir = Heading.ToFloat2(cm.LastHeading);
                            dest.Value = l2w.Position.xy + (dir.Equals(float2.zero) ? l2w.Up.xy : dir);
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
                if (cm.LastChoice != selectedChoice)
                {
                    //Logger.LogIf(entity.Index == 9, $"New Choice was: {selectedChoice}");
                    cm.LastChoice = selectedChoice;
                    cm.LastChoiceTime = Time;
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