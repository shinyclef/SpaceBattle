using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(MainGameGroup))]
[UpdateAfter(typeof(NearestEnemySys))]
public class CombatAiSys : JobComponentSystem
{
    private const float RefreshNearestEnemiesInterval = 1f;

    public bool EnableDebugRays;

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        inputDeps = new Job()
        {
            NearbyEnemyBufs = GetBufferFromEntity<NearbyEnemyBuf>(true),
            L2WComps = GetComponentDataFromEntity<LocalToWorld>(true),
            UtilityScoreBufs = GetBufferFromEntity<UtilityScoreBuf>(false),
            Decisions = AiDataSys.NativeData.Decisions,
            Choices = AiDataSys.NativeData.Choices,
            Considerations = AiDataSys.NativeData.Considerations,
            RecordedScores = AiDataSys.NativeData.RecordedScores,
            RecordedDecision = AiInspector.RecordedDecision,
            Time = Time.time,
            RecordedEntity = SelectionSys.SelectedEntity
        }.Schedule(this, inputDeps);

        return inputDeps;
    }

    [BurstCompile]
    private struct Job : IJobForEachWithEntity<NearestEnemy, CombatTarget, LocalToWorld, CombatAi>
    {
        [ReadOnly] public BufferFromEntity<NearbyEnemyBuf> NearbyEnemyBufs;
        [ReadOnly] public ComponentDataFromEntity<LocalToWorld> L2WComps;
        [NativeDisableParallelForRestriction] public BufferFromEntity<UtilityScoreBuf> UtilityScoreBufs;
        [ReadOnly] public NativeArray<Decision> Decisions;
        [ReadOnly] public NativeArray<Choice> Choices;
        [ReadOnly] public NativeArray<Consideration> Considerations;
        [NativeDisableParallelForRestriction] [WriteOnly] public NativeArray<float> RecordedScores;
        public DecisionType RecordedDecision;

        public float Time;
        public Entity RecordedEntity;

        [NativeDisableParallelForRestriction] private DynamicBuffer<UtilityScoreBuf> utilityScores;

        //private int eId;

        public void Execute(Entity entity, int index,
            [ReadOnly] ref NearestEnemy nearestEnemy,
            [ReadOnly] ref CombatTarget target,
            [ReadOnly] ref LocalToWorld l2w,
            ref CombatAi ai)
        {
            // if I have no target candidates available, request nearest enemies refresh
            if (nearestEnemy.BufferEntity == Entity.Null)
            {
                nearestEnemy.UpdateRequired = true;
                if (ai.ActiveChoice != ChoiceType.None)
                {
                    ai.ActiveChoice = ChoiceType.None;
                    ai.ChoiceSelectedTime = Time;
                    target.Entity = Entity.Null;
                }

                return;
            }

            if (Time - ai.LastEvalTime < 0.3f && L2WComps.Exists(target.Entity))
            {
                return;
            }

            // I haven't refreshed targets in a while, request nearest enemies refresh
            if (Time - nearestEnemy.LastUpdatedTime > RefreshNearestEnemiesInterval)
            {
                nearestEnemy.UpdateRequired = true;
            }

            NativeArray<NearbyEnemyBuf> enemies = NearbyEnemyBufs[nearestEnemy.BufferEntity].AsNativeArray();

            //eId = entity.Index;
            ai.LastEvalTime = Time;

            //float2 targPos = target.Pos;
            float distance = 0f;
            bool hasDistance = false;
            float angle = 0f;
            bool hasAngle = false;

            // make decision
            utilityScores = UtilityScoreBufs[entity];
            DecisionMaker dm = new DecisionMaker(ref Decisions, ref Choices, ref Considerations, ref utilityScores, Time,
                ai.ActiveChoice, ref RecordedScores, RecordedDecision, entity == RecordedEntity);
            bool hasNext = dm.PrepareDecision(DecisionType.CombatMovement);
            while (hasNext)
            {
                FactType requiredFact = dm.NextRequiredFactType;
                // Logger.LogIf(entity.Index == 9, $"Required Fact: {requiredFact}");
                NativeArray<float> factValue = new NativeArray<float>(1, Allocator.Temp);
                switch (requiredFact)
                {
                    case FactType.DistanceFromTargetMulti:
                        if (!hasDistance)
                        {
                            hasDistance = true;
                            distance = math.distance(l2w.Position.xy, L2WComps[enemies[0]].Position.xy);
                        }

                        factValue[0] = distance;
                        break;

                    case FactType.AngleFromTargetMulti:
                        if (!hasAngle)
                        {
                            hasAngle = true;
                            float2 dirToEnemy = math.normalizesafe(L2WComps[enemies[0]].Position.xy - l2w.Position.xy);
                            angle = gmath.AngleBetweenVectors(dirToEnemy, l2w.Up.xy);
                        }

                        factValue[0] = angle;
                        break;

                    case FactType.Noise:
                        factValue[0] = ai.NoiseSeed;
                        break;

                    case FactType.TimeSinceLastChoiceSelection:
                        factValue[0] = Time - ai.ChoiceSelectedTime;
                        break;

                    case FactType.TimeSinceThisChoiceSelection:
                        factValue[0] = dm.CurrentlyEvaluatedChoice == ai.ActiveChoice ? Time - ai.ChoiceSelectedTime : 0f;
                        break;

                    default:
                        //Logger.LogIf(entity.Index == 9, $"I'm unable to provide FactType: {requiredFact}");
                        factValue[0] = 0f;
                        break;
                }

                hasNext = dm.EvaluateNextConsideration(factValue);
            }

            ChoiceType selectedChoice = dm.SelectedChoice;
            if (selectedChoice == ChoiceType.None)
            {
                target.Entity = Entity.Null;
            }
            else
            {
                target.Entity = enemies[dm.SelectedTarget];
                target.Pos = L2WComps[enemies[0]].Position.xy;
            }

            utilityScores.Clear();

            //Logger.LogIf(entity.Index == 1032, $"selectedChoice was: {selectedChoice}. Target: {dm.SelectedTarget}");

            if (ai.ActiveChoice != selectedChoice)
            {
                //Logger.LogIf(entity.Index == 9, $"New Choice was: {selectedChoice}");
                ai.ActiveChoice = selectedChoice;
                ai.ChoiceSelectedTime = Time;
            }
        }
    }
}