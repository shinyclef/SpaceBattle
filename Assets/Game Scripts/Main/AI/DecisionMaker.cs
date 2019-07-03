using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct DecisionMaker
{
    public NativeArray<Decision> Decisions;
    public NativeArray<Choice> Choices;
    public NativeArray<Consideration> Considerations;
    public DynamicBuffer<UtilityScoreBuf> UtilityScores;
    public NativeArray<int> ChoiceTargetCounts;
    public ChoiceType CurrentChoice;
    public float Time;
    public NativeHashMap<int, float> RecordedScores;
    public DecisionType RecordedDecision;
    private bool RecordedEntity;

    private NativeArray<float> currentChoiceScores;
    private float bestChoiceScore;
    private Choice choice;
    private float modificationFactor;

    public ushort ChoiceIndexFrom;
    public ushort ChoiceIndexTo;
    public ushort ChoiceIndex;

    private ushort considerationIndexFrom;
    private ushort considerationIndexTo;
    private ushort considerationIndex;

    private bool record;

    public int CurrentChoiceTargetCount { get { return ChoiceTargetCounts[ChoiceIndex - ChoiceIndexFrom]; } }
    public FactType NextRequiredFactType { get; private set; }
    public ChoiceType CurrentlyEvaluatedChoice { get; private set; }
    public ChoiceType SelectedChoice { get; private set; }
    public int SelectedTarget { get; private set; }
    private int ChoiceNum { get { return ChoiceIndex - ChoiceIndexFrom + 1; } }
    private int ConsiderationNum { get { return considerationIndex - considerationIndexFrom + 1; } }
    private int ChoiceRecordKey { get { return ChoiceNum * 100000; } }
    private int ConsiderationRecordKey { get { return ChoiceNum * 100000 + ConsiderationNum * 1000; } }

    public DecisionMaker(ref NativeArray<Decision> decisions, ref NativeArray<Choice> choices, ref NativeArray<Consideration> considerations, 
        ref DynamicBuffer<UtilityScoreBuf> utilityScores, float time, ChoiceType currentChoice, 
        ref NativeHashMap<int, float> recordedScores, DecisionType decisionType, DecisionType recordedDecision, bool recordedEntity) : this()
    {
        Decisions = decisions;
        Choices = choices;
        Considerations = considerations;
        UtilityScores = utilityScores;
        Time = time;
        CurrentChoice = currentChoice;
        RecordedScores = recordedScores;
        RecordedDecision = recordedDecision;
        RecordedEntity = recordedEntity;

        ChoiceIndexFrom = Decisions[(int)decisionType].ChoiceIndexStart;
        ChoiceIndexTo = (int)decisionType < Decisions.Length - 1 ? Decisions[(int)decisionType + 1].ChoiceIndexStart : (ushort)Choices.Length;
        ChoiceIndex = ChoiceIndexFrom;
        ChoiceTargetCounts = new NativeArray<int>(ChoiceIndexTo - ChoiceIndexFrom, Allocator.Temp);
    }

    public bool PrepareDecision(DecisionType decisionType)
    {
        if (Decisions.Length == 0 || ChoiceIndexFrom == ChoiceIndexTo)
        {
            return false;
        }

        bestChoiceScore = 0f;
        record = RecordedEntity && decisionType == RecordedDecision;

        if (!PrepareChoiceVariables())
        {
            if (!FinaliseChoiceAndMoveToNext())
            {
                return false;
            }
        }

        NextRequiredFactType = Considerations[considerationIndex].FactType;
        CurrentlyEvaluatedChoice = Choices[ChoiceIndex].ChoiceType;
        return true;
    }

    public bool EvaluateNextConsideration(NativeArray<float> factValues)
    {
        // evluate the considertion and multiply the result with the factValue
        Consideration consideration = Considerations[considerationIndex];

        for (int i = 0; i < factValues.Length; i++)
        {
            // normalize input
            float factValue = consideration.GetNormalizedInput(factValues[i], Time);
            float score = consideration.Evaluate(factValue);
            if (record)
            {
                RecordedScores.TryAdd(ConsiderationRecordKey + i * 10, score);
                RecordedScores.TryAdd(ConsiderationRecordKey + i * 10 + 1, factValue);
                if (factValues.Length == 1)
                {
                    for (int j = 1; j < CurrentChoiceTargetCount; j++)
                    {
                        RecordedScores.TryAdd(ConsiderationRecordKey + j * 10, score);
                        RecordedScores.TryAdd(ConsiderationRecordKey + j * 10 + 1, factValue);
                    }
                }
            }

            float makeUpValue = (1 - score) * modificationFactor;
            //Logger.Log($"factValue: {factValue} ({factValueRaw}), score: {score}, makeUpValue: {makeUpValue}");
            score += makeUpValue * score;
            currentChoiceScores[i] *= score;
            if (factValues.Length == 1)
            {
                for (int j = 1; j < CurrentChoiceTargetCount; j++)
                {
                    currentChoiceScores[j] *= score;
                }
            }
        }

        // if this is the last last consideration of the choice, or we're below minRequiredChoiceScore, move on the next choice.
        bool allChoiceScoresBelowBest = AllChoiceScoresAreBelowThreshold(bestChoiceScore);
        if (allChoiceScoresBelowBest)
        {
            if (record)
            {
                if (considerationIndex == considerationIndexTo - 1)
                {
                    // we've finished recording all considerations, now record the final choice scores
                    for (int i = 0; i < CurrentChoiceTargetCount; i++)
                    {
                        RecordedScores.TryAdd(ChoiceRecordKey + i * 10, currentChoiceScores[i]);
                    }
                }
            }
            else
            {
                considerationIndex = (ushort)(considerationIndexTo - 1); // move to the next choice by saying we just finished the final consideration
            }

            //Logger.Log($"Score cutooff. currentChoiceScore: {currentChoiceScore}, minRequiredChoiceScore: {minRequiredChoiceScore}");
        }

        considerationIndex++;
        if (considerationIndex == considerationIndexTo)
        {
            // we have completed all considerations

            if (record && !allChoiceScoresBelowBest)
            {
                for (int i = 0; i < CurrentChoiceTargetCount; i++)
                {
                    RecordedScores.TryAdd(ChoiceRecordKey + i * 10, currentChoiceScores[i]);
                }
            }

            for (int i = 0; i < currentChoiceScores.Length; i++)
            {
                bestChoiceScore = math.max(currentChoiceScores[i], bestChoiceScore);
            }

            // we've completed this consideration, let's score it and look at the next choice
            if (!FinaliseChoiceAndMoveToNext())
            {
                SelectChoiceHighestScore();
                return false;
            }
        }

        NextRequiredFactType = Considerations[considerationIndex].FactType;
        CurrentlyEvaluatedChoice = Choices[ChoiceIndex].ChoiceType;
        return true;
    }

    private bool PrepareChoiceVariables()
    {
        choice = Choices[ChoiceIndex];
        considerationIndexFrom = choice.ConsiderationIndexStart;
        considerationIndexTo = ChoiceIndex < Choices.Length - 1 ? Choices[ChoiceIndex + 1].ConsiderationIndexStart : (ushort)Considerations.Length;
        if (considerationIndexFrom == considerationIndexTo)
        {
            return false;
        }

        considerationIndex = considerationIndexFrom;
        modificationFactor = 1f - (1f / (considerationIndexTo - considerationIndexFrom));

        currentChoiceScores = new NativeArray<float>(CurrentChoiceTargetCount, Allocator.Temp);
        SetAllCurrentChoiceVals(currentChoiceScores.Length, choice.Weight + math.select(0f, choice.MomentumFactor, choice.ChoiceType == CurrentChoice));
        return true;
    }

    private bool FinaliseChoiceAndMoveToNext()
    {
        // finalise choice
        for (int i = 0; i < currentChoiceScores.Length; i++)
        {
            UtilityScores.Add(currentChoiceScores[i]);
        }

        // move to next choice
        ChoiceIndex++;
        if (ChoiceIndex == ChoiceIndexTo)
        {
            return false; // there are no choices left to evaluate
        }

        if (!PrepareChoiceVariables())
        {
            return FinaliseChoiceAndMoveToNext();
        }

        return true;
    }

    private void SetAllCurrentChoiceVals(int targetCount, float val)
    {
        for (int i = 0; i < targetCount; i++)
        {
            currentChoiceScores[i] = val;
        }
    }

    private bool AllChoiceScoresAreBelowThreshold(float threshold)
    {
        for (int i = 0; i < CurrentChoiceTargetCount; i++)
        {
            if (currentChoiceScores[i] >= threshold)
            {
                return false;
            }
        }

        return true;
    }

    private void SelectChoiceHighestScore()
    {
        ChoiceIndex = ChoiceIndexFrom;
        choice = Choices[ChoiceIndex];
        int target = -1;

        float max = 0;
        for (int i = 0; i < UtilityScores.Length; i++)
        {
            if (target == CurrentChoiceTargetCount - 1)
            {
                ChoiceIndex++;
                target = 0;
            }
            else
            {
                target++;
            }

            if (UtilityScores[i].Value > max)
            {
                max = UtilityScores[i].Value;
                SelectedChoice = Choices[ChoiceIndex].ChoiceType;
                SelectedTarget = target;
            }
        }

        if (max == 0)
        {
            //Logger.Log($"Heads up, I'm selecting default choice! Max was {max}.");
            SelectedChoice = ChoiceType.None;
        }
    }
}