using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct DecisionMaker
{
    public NativeArray<Decision> Decisions;
    public NativeArray<Choice> Choices;
    public NativeArray<Consideration> Considerations;
    public DynamicBuffer<UtilityScoreBuf> UtilityScores;
    public ChoiceType CurrentChoice;
    public float Time;
    public NativeArray<float> RecordedScores;
    public DecisionType RecordedDecision;
    private bool RecordedEntity;

    private float minRequiredChoiceScore;
    private NativeArray<float> currentChoiceScores;
    private float bestChoiceScore;
    private Decision decision;
    private Choice choice;
    private float modificationFactor;

    private ushort choiceIndexFrom;
    private ushort choiceIndexTo;
    private ushort choiceIndex;

    private ushort considerationIndexFrom;
    private ushort considerationIndexTo;
    private ushort considerationIndex;

    private bool record;
    private int recordIndex;

    public DecisionMaker(ref NativeArray<Decision> decisions, ref NativeArray<Choice> choices, ref NativeArray<Consideration> considerations, 
        ref DynamicBuffer<UtilityScoreBuf> utilityScores, float time, ChoiceType currentChoice, ref NativeArray<float> recordedScores, DecisionType recordedDecision, bool recordedEntity) : this()
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
    }

    public FactType NextRequiredFactType { get; private set; }

    public ChoiceType CurrentlyEvaluatedChoice { get; private set; }

    public ChoiceType SelectedChoice { get; private set; }
    public int SelectedTarget { get; private set; }

    public bool PrepareDecision(DecisionType decisionType)
    {
        if (Decisions.Length == 0)
        {
            return false;
        }

        choiceIndexFrom = Decisions[(int)decisionType].ChoiceIndexStart;
        choiceIndexTo = (int)decisionType < Decisions.Length - 1 ? Decisions[(int)decisionType + 1].ChoiceIndexStart : (ushort)Choices.Length;
        if (choiceIndexFrom == choiceIndexTo)
        {
            return false;
        }

        choiceIndex = choiceIndexFrom;
        bestChoiceScore = 0f;
        decision = Decisions[(int)decisionType];
        minRequiredChoiceScore = 0f;
        recordIndex = 0;
        record = RecordedEntity && decisionType == RecordedDecision;

        if (!PrepareChoiceVariables())
        {
            // we're skipping this choice
            if (record)
            {
                // still need to record the skipped choice
                for (int i = 0; i < currentChoiceScores.Length; i++)
                {
                    RecordedScores[recordIndex] = 0f;
                    recordIndex++;
                }
            }

            if (!FinaliseChoiceAndMoveToNext())
            {
                return false;
            }
        }

        NextRequiredFactType = Considerations[considerationIndex].FactType;
        CurrentlyEvaluatedChoice = Choices[choiceIndex].ChoiceType;
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
                RecordedScores[recordIndex] = score;
                RecordedScores[recordIndex + 1] = factValue;
                recordIndex += 2;
            }

            float makeUpValue = (1 - score) * modificationFactor;
            //Logger.Log($"factValue: {factValue} ({factValueRaw}), score: {score}, makeUpValue: {makeUpValue}");
            score += makeUpValue * score;
            currentChoiceScores[i] *= score;
        }

        // if this is the last last consideration of the choice, or we're below minRequiredChoiceScore, move on the next choice.
        bool allChoiceScoresBelowMin = AllChoiceScoresAreBelowThreshold(minRequiredChoiceScore);
        if (allChoiceScoresBelowMin)
        {
            if (record)
            {
                if (considerationIndex == considerationIndexTo - 1)
                {
                    // we've finished recording all considerations, now record the final choice scores
                    for (int i = 0; i < currentChoiceScores.Length; i++)
                    {
                        RecordedScores[recordIndex] = currentChoiceScores[i];
                        recordIndex++;
                    }
                }
            }
            else
            {
                considerationIndex = (ushort)(considerationIndexTo - 1); // move to the next choice by saying we just finished the final choice
            }

            //Logger.Log($"Score cutooff. currentChoiceScore: {currentChoiceScore}, minRequiredChoiceScore: {minRequiredChoiceScore}");
            SetAllCurrentChoiceVals(0f);
        }

        considerationIndex++;
        if (considerationIndex == considerationIndexTo)
        {
            // we have completed all considerations

            if (record && !allChoiceScoresBelowMin)
            {
                for (int i = 0; i < currentChoiceScores.Length; i++)
                {
                    RecordedScores[recordIndex] = currentChoiceScores[i];
                    recordIndex++;
                }
            }

            for (int i = 0; i < currentChoiceScores.Length; i++)
            {
                bestChoiceScore = math.max(currentChoiceScores[i], bestChoiceScore);
            }

            minRequiredChoiceScore = bestChoiceScore * decision.MinimumRequiredOfBest;

            // we've completed this consideration, let's score it and look at the next choice
            if (!FinaliseChoiceAndMoveToNext())
            {
                SelectChoiceHighestScore();
                return false;
            }
        }

        NextRequiredFactType = Considerations[considerationIndex].FactType;
        CurrentlyEvaluatedChoice = Choices[choiceIndex].ChoiceType;
        return true;
    }

    private bool PrepareChoiceVariables()
    {
        choice = Choices[choiceIndex];
        considerationIndexFrom = choice.ConsiderationIndexStart;
        considerationIndexTo = choiceIndex < Choices.Length - 1 ? Choices[choiceIndex + 1].ConsiderationIndexStart : (ushort)Considerations.Length;
        if (considerationIndexFrom == considerationIndexTo)
        {
            return false;
        }

        considerationIndex = considerationIndexFrom;
        modificationFactor = 1f - (1f / (considerationIndexTo - considerationIndexFrom));

        currentChoiceScores = new NativeArray<float>(choice.TargetCount, Allocator.Temp);
        SetAllCurrentChoiceVals(choice.Weight + math.select(0f, choice.MomentumFactor, choice.ChoiceType == CurrentChoice));
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
        choiceIndex++;
        if (choiceIndex == choiceIndexTo)
        {
            return false; // there are no choices left to evaluate
        }

        if (!PrepareChoiceVariables())
        {
            // we're skipping this choice
            if (record)
            {
                // still need to record the skipped choice
                for (int i = 0; i < currentChoiceScores.Length; i++)
                {
                    RecordedScores[recordIndex] = 0f;
                    recordIndex++;
                }
            }

            return FinaliseChoiceAndMoveToNext();
        }

        return true;
    }

    private void SetAllCurrentChoiceVals(float val)
    {
        for (int i = 0; i < currentChoiceScores.Length; i++)
        {
            currentChoiceScores[i] = val;
        }
    }

    private bool AllChoiceScoresAreBelowThreshold(float threshold)
    {
        for (int i = 0; i < currentChoiceScores.Length; i++)
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
        int currentChoiceIndex = choiceIndexFrom;
        choice = Choices[currentChoiceIndex];
        SelectedTarget = -1;

        float max = 0;
        for (int i = 0; i < UtilityScores.Length; i++)
        {
            if (SelectedTarget == choice.TargetCount - 1)
            {
                currentChoiceIndex++;
                SelectedTarget = 0;
            }
            else
            {
                SelectedTarget++;
            }

            if (UtilityScores[i].Value > max)
            {
                max = UtilityScores[i].Value;
                SelectedChoice = Choices[currentChoiceIndex].ChoiceType;
            }
        }

        if (max == 0)
        {
            //Logger.Log($"Heads up, I'm selecting default choice! Max was {max}.");
            SelectedChoice = ChoiceType.None;
        }
    }
}