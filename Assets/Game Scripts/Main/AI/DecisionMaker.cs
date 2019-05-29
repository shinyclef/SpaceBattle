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
    private float grandTotalScore;
    private float currentChoiceScore;
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

    public ChoiceType SelectedChoice { get; private set; }

    public bool PrepareDecision(DecisionType decisionType, ref Random rand)
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
        grandTotalScore = 0f;
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
                RecordedScores[recordIndex] = currentChoiceScore;
                recordIndex++;
            }

            if (!FinaliseChoiceAndMoveToNext())
            {
                return false;
            }
        }

        NextRequiredFactType = Considerations[considerationIndex].FactType;
        return true;
    }

    public bool EvaluateNextConsideration(float factValueRaw, ref Random rand)
    {
        // evluate the considertion and multiply the result with the factValue
        Consideration consideration = Considerations[considerationIndex];

        // normalize input
        float factValue = consideration.GetNormalizedInput(factValueRaw);
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
        currentChoiceScore *= score;
        
        // if this is the last last consideration of the choice, or we're below minRequiredChoiceScore, move on the next choice.
        if (currentChoiceScore < minRequiredChoiceScore)
        {
            if (record)
            {
                if (considerationIndex == considerationIndexTo - 1)
                {
                    RecordedScores[recordIndex] = currentChoiceScore;
                    recordIndex++;
                }
            }
            else
            {
                considerationIndex = (ushort)(considerationIndexTo - 1); // move to the next choice by saying we just finished the final choice
            }
            
            //Logger.Log($"Score cutooff. currentChoiceScore: {currentChoiceScore}, minRequiredChoiceScore: {minRequiredChoiceScore}");
            currentChoiceScore = 0f;
        }

        considerationIndex++;
        if (considerationIndex == considerationIndexTo)
        {
            if (record && currentChoiceScore >= minRequiredChoiceScore)
            {
                RecordedScores[recordIndex] = currentChoiceScore;
                recordIndex++;
            }

            bestChoiceScore = math.max(currentChoiceScore, bestChoiceScore);
            minRequiredChoiceScore = bestChoiceScore * decision.MinimumRequiredOfBest;

            // we've completed this consideration, let's score it and look at the next choice
            if (!FinaliseChoiceAndMoveToNext())
            {
                SelectChoiceRandomVariance(ref rand);
                return false;
            }
        }

        NextRequiredFactType = Considerations[considerationIndex].FactType;
        return true;
    }

    private bool PrepareChoiceVariables()
    {
        currentChoiceScore = 0;
        choice = Choices[choiceIndex];
        considerationIndexFrom = choice.ConsiderationIndexStart;
        considerationIndexTo = choiceIndex < Choices.Length - 1 ? Choices[choiceIndex + 1].ConsiderationIndexStart : (ushort)Considerations.Length;
        if (considerationIndexFrom == considerationIndexTo)
        {
            return false;
        }

        considerationIndex = considerationIndexFrom;
        modificationFactor = 1f - (1f / (considerationIndexTo - considerationIndexFrom));
        currentChoiceScore = choice.Weight + math.select(0f, choice.MomentumFactor, choice.ChoiceType == CurrentChoice);
        return true;
    }

    private bool FinaliseChoiceAndMoveToNext()
    {
        // finalise choice
        UtilityScores.Add(currentChoiceScore);
        grandTotalScore += currentChoiceScore;

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
                // still need to recorded the skipped choice
                RecordedScores[recordIndex] = currentChoiceScore;
                recordIndex++;
            }

            return FinaliseChoiceAndMoveToNext();
        }

        return true;
    }

    private void SelectChoiceHighestScore()
    {
        float max = 0;
        for (int i = 0; i < UtilityScores.Length; i++)
        {
            if (UtilityScores[i].Value > max)
            {
                max = UtilityScores[i].Value;
                SelectedChoice = Choices[choiceIndexFrom + i].ChoiceType;
            }
        }

        if (max == 0)
        {
            //Logger.Log($"Heads up, I'm selecting default choice! Max was {max}.");
            SelectedChoice = ChoiceType.None;
        }
    }

    private void SelectChoiceNoiseOffset(half noiseSeed, half noiseWaveLen)
    {
        float max = float.MinValue;
        for (int i = 0; i < UtilityScores.Length; i++)
        {
            float noiseOffset = noise.snoise(new float2(noiseSeed, Time * noiseWaveLen));
            if (UtilityScores[i].Value + noiseOffset > max)
            {
                max = UtilityScores[i].Value + noiseOffset;
                SelectedChoice = Choices[choiceIndexFrom + i].ChoiceType;
            }
        }

        if (max == float.MinValue)
        {
            //Logger.Log($"Heads up, I'm selecting default choice! Max was {max}.");
            SelectedChoice = ChoiceType.None;
        }
    }

    private void SelectChoiceRandomVariance(ref Random rand)
    {
        float max = -100;
        for (int i = 0; i < UtilityScores.Length; i++)
        {
            float randVariance = 0.5f;
            float randOffset = rand.NextFloat() * randVariance - (0.5f * randVariance);
            if (UtilityScores[i].Value + randOffset > max)
            {
                max = UtilityScores[i].Value + randOffset;
                SelectedChoice = Choices[choiceIndexFrom + i].ChoiceType;
            }
        }

        if (max == -100)
        {
            //Logger.Log($"Heads up, I'm selecting default choice! Max was {max}.");
            SelectedChoice = ChoiceType.None;
        }
    }

    private void SelectChoiceWeightedRandom(ref Random rand)
    {
        float max = 0;
        for (int i = 0; i < UtilityScores.Length; i++)
        {
            max += UtilityScores[i].Value / grandTotalScore;
            if (rand.NextFloat() < max)
            {
                SelectedChoice = Choices[choiceIndexFrom + i].ChoiceType;
                return;
            }
        }

        //Logger.Log($"Heads up, I'm selecting default choice! Max was {max}.");
        SelectedChoice = ChoiceType.None;
    }
}