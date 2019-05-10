using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(MainGameGroup))]
[UpdateAfter(typeof(CombatTargetSys))]
public class CombatMovementAiSys : JobComponentSystem
{
    private NativeArray<Choice> choices;
    private NativeArray<int> considerationIndecies;
    private NativeArray<Consideration> considerations;
    private NativeArray<Random> rngs;

    protected override void OnCreate()
    {
        rngs = new NativeArray<Random>(JobsUtility.MaxJobThreadCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        for (int i = 0; i < JobsUtility.MaxJobThreadCount; i++)
        {
            rngs[i] = Rand.New();
        }

        PrepareData();
    }

    private void PrepareData()
    {
        choices = new NativeArray<Choice>(2, Allocator.Persistent);
        considerationIndecies = new NativeArray<int>(2, Allocator.Persistent);
        considerations = new NativeArray<Consideration>(2, Allocator.Persistent);

        considerations[0] = new Consideration
        {
            FactType = FactType.DistanceFromTarget,
            GraphType = GraphType.Linear,
            Slope = 0.001f,
            YShift = -0.01f
        };

        considerations[1] = new Consideration
        {
            FactType = FactType.DistanceFromTarget,
            GraphType = GraphType.Exponential,
            Slope = 1.35f,
            Exp = -0.5f,
            XShift = 10f,
            YShift = -0.15f
        };

        considerationIndecies[0] = 0;
        considerationIndecies[1] = 1;

        choices[0] = new Choice
        {
            ChoiceType = ChoiceType.FlyTowardsEnemy,
            ConsiderationIndexStart = 0,
            Weight = 1
        };

        choices[1] = new Choice
        {
            ChoiceType = ChoiceType.FlyAwayFromEnemy,
            ConsiderationIndexStart = 1,
            Weight = 1
        };
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var job = new Job()
        {
            Rngs = rngs,
            UtilityScoreBufs = GetBufferFromEntity<UtilityScoreBuf>(),
            TranslationComps = GetComponentDataFromEntity<Translation>(),
            Choices = choices,
            ConsiderationIndecies = considerationIndecies,
            Considerations = considerations
        };

        JobHandle jh = job.Schedule(this, inputDeps);
        return jh;
    }

    [BurstCompile]
    private struct Job : IJobForEachWithEntity<CombatTarget, LocalToWorld, MoveDestination>
    {
        [NativeDisableContainerSafetyRestriction] public NativeArray<Random> Rngs;
        [NativeDisableParallelForRestriction] public BufferFromEntity<UtilityScoreBuf> UtilityScoreBufs;
        [ReadOnly] public ComponentDataFromEntity<Translation> TranslationComps;
        [ReadOnly] public NativeArray<Choice> Choices;
        [ReadOnly] public NativeArray<int> ConsiderationIndecies;
        [ReadOnly] public NativeArray<Consideration> Considerations;

        [NativeSetThreadIndex] private int threadId;
        [NativeDisableParallelForRestriction] private DynamicBuffer<UtilityScoreBuf> utilityScores;
        private float2 targetPos;
        private float distanceFromTarget;

        public void Execute(Entity entity, int index,
            [ReadOnly] ref CombatTarget enemy,
            [ReadOnly] ref LocalToWorld l2w,
            ref MoveDestination dest)
        {
            if (enemy.Value != Entity.Null && TranslationComps.Exists(enemy.Value))
            {
                utilityScores = UtilityScoreBufs[entity];
                targetPos = TranslationComps[enemy.Value].Value.xy;
                distanceFromTarget = math.distance(l2w.Position.xy, targetPos);

                // make decision
                float totalScore = EvaluateChoices(ref l2w);

                Random rng = Rngs[threadId];
                float r = rng.NextFloat();
                Rngs[threadId] = rng;

                int selectedChoiceIndex = UtilityAi.SelectChoice(totalScore, r, ref utilityScores);
                utilityScores.Clear();
                ChoiceType selectedChoice = Choices[selectedChoiceIndex].ChoiceType;
                //Logger.Log("Selected Choice is: " + selectedChoice);

                switch (selectedChoice)
                {
                    case ChoiceType.FlyTowardsEnemy:
                        dest.Value = CombatFlyToward(targetPos);
                        dest.IsCombatTarget = true;
                        break;
                    case ChoiceType.FlyAwayFromEnemy:
                        dest.Value = CombatFlyAway(targetPos);
                        dest.IsCombatTarget = false;
                        break;
                    default:
                        dest.Value = l2w.Position.xy + l2w.Up.xy;
                        dest.IsCombatTarget = false;
                        return;
                }

                dest.IsCombatTarget = true;
            }
            else
            {
                dest.Value = l2w.Position.xy + l2w.Up.xy;
                dest.IsCombatTarget = false;
            }

            float2 CombatFlyToward(float2 enemyPos)
            {
                return enemyPos;
            }

            float2 CombatFlyAway(float2 enemyPos)
            {
                
                return enemyPos;
            }
        }

        private float EvaluateChoices(ref LocalToWorld l2w)
        {
            float grandTotalScore = 0f;
            float minRequiredScore = 0f;

            for (int i = 0; i < Choices.Length; i++)
            {
                Choice choice = Choices[i];
                float totalScore = choice.Weight + choice.MomentumFactor;
                int indexFrom = choice.ConsiderationIndexStart;
                int indexTo = i < Choices.Length - 1 ? Choices[i + 1].ConsiderationIndexStart : Choices.Length;
                int considerationCount = indexTo - indexFrom;
                float modificationFactor = 1f - (1f / considerationCount);

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
                        default:
                            factValue = 0f;
                            break;
                    }

                    // evluate the considertion and multiply the result with the factValue
                    float score = math.clamp(consideration.Evaluate(factValue), 0f, 1f);
                    float makeUpValue = (1 - score) * modificationFactor;
                    score += makeUpValue * score;
                    totalScore *= score;
                }

                utilityScores.Add(totalScore);
                grandTotalScore += totalScore;
            }

            return grandTotalScore;
        }
    }
}