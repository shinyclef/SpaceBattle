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
    private NativeArray<Choice> choices;
    private NativeArray<int> considerationReferences;
    private NativeArray<Consideration> considerations;
    private NativeArray<Random> rngs;

    protected override void OnCreate()
    {
        rngs = new NativeArray<Random>(JobsUtility.MaxJobThreadCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        var dto = GetTestDto();
        PrepareData(dto, true);
    }

    public void UpdateAi(UtilityAiDto aiDto)
    {
        PrepareData(aiDto, false);
    }

    private void PrepareDataOld()
    {
        choices = new NativeArray<Choice>(2, Allocator.Persistent);
        considerationReferences = new NativeArray<int>(4, Allocator.Persistent);
        considerations = new NativeArray<Consideration>(3, Allocator.Persistent);

        // fly towards enemy considerations
        considerations[0] = new Consideration
        {
            FactType = FactType.DistanceFromTarget,
            GraphType = GraphType.Exponential,
            Slope = -100f,
            Exp = -1.5f,
            XShift = new half(19f),
            YShift = new half(1.2f)
        };

        // fly away from enemy considerations
        considerations[1] = new Consideration
        {
            FactType = FactType.DistanceFromTarget,
            GraphType = GraphType.Exponential,
            Slope = 5f,
            Exp = -1.5f,
            XShift = new half(-1f),
            YShift = new half(-0.01f)
        };

        // considerations for both
        considerations[2] = new Consideration
        {
            FactType = FactType.TimeSinceLastDecision,
            GraphType = GraphType.Exponential,
            Slope = 0.002f,
            Exp = 1.7f,
            XShift = new half(0.1f),
            YShift = new half(-0.004f)
        };

        // choices
        considerationReferences[0] = 0;
        considerationReferences[1] = 2;
        choices[0] = new Choice
        {
            ChoiceType = ChoiceType.FlyTowardEnemy,
            ConsiderationIndexStart = 0,
            Weight = 1
        };


        considerationReferences[2] = 1;
        considerationReferences[3] = 2;
        choices[1] = new Choice
        {
            ChoiceType = ChoiceType.FlyAwayFromEnemy,
            ConsiderationIndexStart = 2,
            Weight = 1
        };
    }

    private void PrepareData(UtilityAiDto dto, bool firstTime)
    {
        if (!firstTime)
        {
            choices.Dispose();
            considerationReferences.Dispose();
            considerations.Dispose();
        }

        int considerationReferenceCount = 0;
        int nextStartIndex = 0;
        for (int i = 0; i < dto.Choices.Length; i++)
        {
            // find out how many consideration references there are
            ChoiceDto cd = dto.Choices[i];
            considerationReferenceCount += cd.ConsiderationIndecies.Length;
        }

        int choiceCount = dto.Choices.Length;
        choices = new NativeArray<Choice>(choiceCount, Allocator.Persistent);
        considerationReferences = new NativeArray<int>(considerationReferenceCount, Allocator.Persistent);
        for (int i = 0; i < dto.Choices.Length; i++)
        {
            // populate choices and consideration references
            ChoiceDto cd = dto.Choices[i];
            
            // populate choices
            choices[i] = new Choice
            {
                ChoiceType = cd.ChoiceType,
                ConsiderationIndexStart = nextStartIndex,
                Weight = cd.Weight,
                MomentumFactor = cd.Momentum
            };

            // populate consideration references (I Need the count first!)
            for (int j = 0; j < cd.ConsiderationIndecies.Length; j++)
            {
                considerationReferences[j] = cd.ConsiderationIndecies[j];
            }

            // increment start index
            nextStartIndex += cd.ConsiderationIndecies.Length;
        }

        int considerationCount = dto.Considerations.Length;
        considerations = new NativeArray<Consideration>(considerationCount, Allocator.Persistent);
        for (int i = 0; i < dto.Considerations.Length; i++)
        {
            ConsiderationDto cd = dto.Considerations[i];
            considerations[i] = new Consideration
            {
                FactType = cd.FactType,
                GraphType = cd.GraphType,
                Slope = cd.Slope,
                Exp = cd.Exp,
                XShift = new half(cd.XShift),
                YShift = new half(cd.YShift),
                InputMin = cd.InputMin,
                InputMax = cd.InputMax
            };
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
            UtilityScoreBufs = GetBufferFromEntity<UtilityScoreBuf>(),
            TranslationComps = GetComponentDataFromEntity<Translation>(),
            Choices = choices,
            ConsiderationIndecies = considerationReferences,
            Considerations = considerations,
            Time = Time.time
        };

        JobHandle jh = job.Schedule(this, inputDeps);
        return jh;
    }

    [BurstCompile]
    private struct Job : IJobForEachWithEntity<CombatTarget, LocalToWorld, MoveDestination, CombatMovement>
    {
        [NativeDisableContainerSafetyRestriction] public NativeArray<Random> Rngs;
        [NativeDisableParallelForRestriction] public BufferFromEntity<UtilityScoreBuf> UtilityScoreBufs;
        [ReadOnly] public ComponentDataFromEntity<Translation> TranslationComps;
        [ReadOnly] public NativeArray<Choice> Choices;
        [ReadOnly] public NativeArray<int> ConsiderationIndecies;
        [ReadOnly] public NativeArray<Consideration> Considerations;

        public float Time;

        #pragma warning disable 0649
        [NativeSetThreadIndex] private int threadId;
        #pragma warning restore 0649
        [NativeDisableParallelForRestriction] private DynamicBuffer<UtilityScoreBuf> utilityScores;
        private float2 targetPos;
        private float distanceFromTarget;

        //private int eId;

        public void Execute(Entity entity, int index,
            [ReadOnly] ref CombatTarget enemy,
            [ReadOnly] ref LocalToWorld l2w,
            ref MoveDestination dest,
            ref CombatMovement cm)
        {
            if (enemy.Value != Entity.Null && TranslationComps.Exists(enemy.Value))
            {
                //eId = entity.Index;
                targetPos = TranslationComps[enemy.Value].Value.xy;
                distanceFromTarget = math.distance(l2w.Position.xy, targetPos);

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
                    float totalScore = EvaluateChoices(ref cm);
                    int selectedChoiceIndex = UtilityAi.SelectChoice(totalScore, ref rand, ref utilityScores);
                    selectedChoice = Choices[selectedChoiceIndex].ChoiceType;
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
                            //Logger.Log("Head: " + Heading.ToFloat2(cm.LastHeading));
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
                    //Logger.LogIf(entity.Index == 9, $"New Choice was: {selectedChoice} at distance {distanceFromTarget}");
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

        private float EvaluateChoices(ref CombatMovement cm)
        {
            float grandTotalScore = 0f;
            float minRequiredScore = 0f;
            for (int i = 0; i < Choices.Length; i++)
            {
                Choice choice = Choices[i];
                float totalScore = choice.Weight + choice.MomentumFactor;

                int indexFrom = choice.ConsiderationIndexStart;
                int indexTo = i < Choices.Length - 1 ? Choices[i + 1].ConsiderationIndexStart : ConsiderationIndecies.Length;

                int considerationCount = indexTo - indexFrom;
                float modificationFactor = 1f - (1f / considerationCount);

                //Logger.LogIf(eId == 9, $" Choices.Length: {Choices.Length}, indexFrom: {indexFrom}, indexTo: {indexTo}");

                for (int j = indexFrom; j < indexTo; j++)
                {
                    if (totalScore <= minRequiredScore)
                    {
                        totalScore = 0f;
                        break;
                    }

                    Consideration consideration = Considerations[ConsiderationIndecies[j]];

                    // get the fact value
                    FactType factType = consideration.FactType;
                    float factValue;
                    switch (factType)
                    {
                        case FactType.DistanceFromTarget:
                            factValue = distanceFromTarget;
                            break;

                        case FactType.AngleFromTarget:
                            factValue = 0f;
                            break;

                        case FactType.TimeSinceLastDecision:
                            //Logger.LogIf(eId == 9, $"LastChoice: {Time - cm.LastChoiceTime}");
                            factValue = choice.ChoiceType == cm.LastChoice ? float.MaxValue : Time - cm.LastChoiceTime;
                                break;
                        default:
                            factValue = 0f;
                            break;
                    }

                    // evluate the considertion and multiply the result with the factValue
                    float score = math.clamp(consideration.Evaluate(factValue), 0f, 1f);
                    float makeUpValue = (1 - score) * modificationFactor;
                    score += makeUpValue * score;
                    totalScore *= score;

                    //Logger.LogIf(eId == 9, $" choice: {i}, con: {j}, score: {score}, makeUpValue: {makeUpValue}, totalScore: {totalScore}");
                }

                
                utilityScores.Add(totalScore);
                grandTotalScore += totalScore;
            }

            return grandTotalScore;
        }
    }

    private UtilityAiDto GetTestDto()
    {
        var dto = new UtilityAiDto
        {
            Choices = new ChoiceDto[]
            {
                new ChoiceDto
                {
                    ChoiceType = ChoiceType.FlyTowardEnemy,
                    Weight = 1f,
                    Momentum = 1f,
                    ConsiderationIndecies = new int[]
                    {
                        0,
                        1
                    }
                },
                new ChoiceDto
                {
                    ChoiceType = ChoiceType.FlyAwayFromEnemy,
                    Weight = 1f,
                    Momentum = 1f,
                    ConsiderationIndecies = new int[]
                    {
                        2,
                        3
                    }
                }
            },
            Considerations = new ConsiderationDto[]
            {
                new ConsiderationDto
                {
                    FactType = FactType.DistanceFromTarget,
                    GraphType = GraphType.Exponential,
                    Slope = -0.6f,
                    Exp = 6f,
                    XShift = 1f,
                    YShift = 0.8f,
                    InputMin = 0f,
                    InputMax = 20f
                },
                new ConsiderationDto
                {
                    FactType = FactType.AngleFromTarget,
                    GraphType = GraphType.Exponential,
                    Slope = 0.6f,
                    Exp = 6f,
                    XShift = 1f,
                    YShift = 0.2f,
                    InputMin = 0f,
                    InputMax = 180f
                },
                new ConsiderationDto
                {
                    FactType = FactType.DistanceFromTarget,
                    GraphType = GraphType.Exponential,
                    Slope = 0.6f,
                    Exp = 6f,
                    XShift = 1f,
                    YShift = 0.2f,
                    InputMin = 0f,
                    InputMax = 20f
                },
                new ConsiderationDto
                {
                    FactType = FactType.AngleFromTarget,
                    GraphType = GraphType.Exponential,
                    Slope = -0.6f,
                    Exp = 6f,
                    XShift = 1f,
                    YShift = 0.8f,
                    InputMin = 0f,
                    InputMax = 180f
                }
            }
        };

        return dto;
    }
}