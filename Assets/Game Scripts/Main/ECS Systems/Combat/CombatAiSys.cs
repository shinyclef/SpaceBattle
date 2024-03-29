﻿using Unity.Burst;
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
    public const float RefreshNearestEnemiesInterval = 1f;
    public bool EnableDebugRays;

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        inputDeps = new Job()
        {
            NearbyEnemyBufs = GetBufferFromEntity<NearbyEnemyBuf>(true),
            L2WComps = GetComponentDataFromEntity<LocalToWorld>(true),
            PhysicsVelocityComps = GetComponentDataFromEntity<PhysicsVelocity>(true),
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
    private struct Job : IJobForEachWithEntity<LocalToWorld, CombatTarget, NearestEnemy, CombatAi, Faction>
    {
        [ReadOnly] public BufferFromEntity<NearbyEnemyBuf> NearbyEnemyBufs;
        [ReadOnly] public ComponentDataFromEntity<LocalToWorld> L2WComps;
        [ReadOnly] public ComponentDataFromEntity<PhysicsVelocity> PhysicsVelocityComps;
        [NativeDisableParallelForRestriction] public BufferFromEntity<UtilityScoreBuf> UtilityScoreBufs;
        [ReadOnly] public NativeArray<Decision> Decisions;
        [ReadOnly] public NativeArray<Choice> Choices;
        [ReadOnly] public NativeArray<Consideration> Considerations;
        [NativeDisableParallelForRestriction] [WriteOnly] public NativeHashMap<int, float> RecordedScores;
        public DecisionType RecordedDecision;

        public float Time;
        public Entity RecordedEntity;

        [NativeDisableParallelForRestriction] private DynamicBuffer<UtilityScoreBuf> utilityScores;

        public void Execute(Entity entity, int index,
            [ReadOnly] ref LocalToWorld l2w,
            ref CombatTarget target,
            ref NearestEnemy nearestEnemy,
            ref CombatAi ai,
            ref Faction f)
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

            bool isRecordedEntity = entity == RecordedEntity;
            if (!isRecordedEntity && Time - ai.LastEvalTime < 0.3f && L2WComps.Exists(target.Entity))
            {
                target.Pos = L2WComps[target.Entity].Position.xy;
                return;
            }

            // I haven't refreshed targets in a while, request nearest enemies refresh
            if (Time - nearestEnemy.LastUpdatedTime > RefreshNearestEnemiesInterval)
            {
                nearestEnemy.UpdateRequired = true;
            }

            if (isRecordedEntity)
            {
                RecordedScores.Clear();
            }
            
            ai.LastEvalTime = Time;
            utilityScores = UtilityScoreBufs[entity];
            NativeArray<float> factValues;
            NativeArray<NearbyEnemyBuf> enemies = NearbyEnemyBufs[nearestEnemy.BufferEntity].AsNativeArray();

            DecisionMaker dm = new DecisionMaker(ref Decisions, ref Choices, ref Considerations, ref utilityScores, Time,
                ai.ActiveChoice, ref RecordedScores, DecisionType.CombatMovement, RecordedDecision, isRecordedEntity);

            for (int i = 0; i < dm.ChoiceTargetCounts.Length; i++)
            {
                switch (Choices[dm.ChoiceIndexFrom + i].ChoiceType)
                {
                    case ChoiceType.FlyTowardsEnemyMulti:
                        dm.ChoiceTargetCounts[i] = enemies.Length;
                        break;

                    default:
                        dm.ChoiceTargetCounts[i] = 1;
                        break;
                }
            }

            bool hasNext = dm.PrepareDecision(DecisionType.CombatMovement);
            //Logger.Log("HasNext: " + hasNext);
            while (hasNext)
            {
                FactType requiredFact = dm.NextRequiredFactType;
                switch (requiredFact)
                {
                    case FactType.DistanceFromTargetMulti:
                        factValues = new NativeArray<float>(enemies.Length, Allocator.Temp);
                        for (int i = 0; i < enemies.Length; i++)
                        {
                            factValues[i] = math.distance(l2w.Position.xy, L2WComps[enemies[i]].Position.xy);
                        }
                        
                        break;

                    case FactType.DistanceFromTargetIn1SecMulti:
                        factValues = new NativeArray<float>(enemies.Length, Allocator.Temp);
                        for (int i = 0; i < enemies.Length; i++)
                        {
                            float2 vel = PhysicsVelocityComps[entity].Linear.xy;
                            float2 targetVel = PhysicsVelocityComps[enemies[i]].Linear.xy;
                            factValues[i] = gmath.DistanceBetweenTwoLines(l2w.Position.xy, vel, L2WComps[enemies[i]].Position.xy, targetVel);
                        }

                        break;

                    case FactType.AngleFromTargetMulti:
                        factValues = new NativeArray<float>(enemies.Length, Allocator.Temp);
                        for (int i = 0; i < enemies.Length; i++)
                        {
                            float2 dirToEnemy = math.normalizesafe(L2WComps[enemies[i]].Position.xy - l2w.Position.xy);
                            factValues[i] = gmath.AngleBetweenVectors(dirToEnemy, math.normalize(l2w.Up.xy));
                            //Logger.Log($"factValues[i]: {factValues[i]}");
                        }

                        break;

                    case FactType.Noise:
                        factValues = new NativeArray<float>(1, Allocator.Temp);
                        factValues[0] = ai.NoiseSeed;
                        break;

                    case FactType.TimeSinceLastChoiceSelection:
                        factValues = new NativeArray<float>(1, Allocator.Temp);
                        factValues[0] = Time - ai.ChoiceSelectedTime;
                        break;

                    case FactType.TimeSinceThisChoiceSelection:
                        factValues = new NativeArray<float>(1, Allocator.Temp);
                        factValues[0] = dm.CurrentlyEvaluatedChoice == ai.ActiveChoice ? Time - ai.ChoiceSelectedTime : 0f;
                        break;

                    default:
                        factValues = new NativeArray<float>(1, Allocator.Temp);
                        factValues[0] = 0f;
                        break;
                }

                hasNext = dm.EvaluateNextConsideration(factValues);
            }

            ChoiceType selectedChoice = dm.SelectedChoice;
            if (selectedChoice == ChoiceType.None)
            {
                target.Entity = Entity.Null;
            }
            else
            {
                target.Entity = enemies[dm.SelectedTarget];
                target.Pos = L2WComps[target.Entity].Position.xy;
            }

            utilityScores.Clear();

            if (ai.ActiveChoice != selectedChoice)
            {
                ai.ActiveChoice = selectedChoice;
                ai.ChoiceSelectedTime = Time;
            }
        }
    }
}