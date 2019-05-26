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
    public NativeArray<float> RecordedScores;
    public DecisionType RecordedDecision;
    private bool RecordedEntity;

    private Random rand;
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
        ref DynamicBuffer<UtilityScoreBuf> utilityScores, ChoiceType currentChoice, ref NativeArray<float> recordedScores, DecisionType recordedDecision, bool recordedEntity) : this()
    {
        Decisions = decisions;
        Choices = choices;
        Considerations = considerations;
        UtilityScores = utilityScores;
        CurrentChoice = currentChoice;
        RecordedScores = recordedScores;
        RecordedDecision = recordedDecision;
        RecordedEntity = recordedEntity;
    }

    public FactType NextRequiredFactType { get; private set; }

    public ChoiceType SelectedChoice { get; private set; }

    public void PrepareDecision(DecisionType decisionType, ref Random rand)
    {
        choiceIndexFrom = Decisions[(int)decisionType].ChoiceIndexStart;
        choiceIndexTo = (int)decisionType < Decisions.Length - 1 ? Decisions[(int)decisionType + 1].ChoiceIndexStart : (ushort)Choices.Length;
        choiceIndex = choiceIndexFrom;

        grandTotalScore = 0f;
        bestChoiceScore = 0f;
        decision = Decisions[(int)decisionType];
        minRequiredChoiceScore = 0f;

        this.rand = rand;
        PrepareChoiceVariables();
        NextRequiredFactType = Considerations[considerationIndex].FactType;

        record = RecordedEntity && decisionType == RecordedDecision;
        recordIndex = 0;
    }

    public bool EvaluateNextConsideration(float factValueRaw)
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
            if (!MoveToNextChoice())
            {
                SelectChoiceHighestScore();
                return false;
            }
        }

        NextRequiredFactType = Considerations[considerationIndex].FactType;
        return true;
    }

    private void PrepareChoiceVariables()
    {
        choice = Choices[choiceIndex];
        considerationIndexFrom = choice.ConsiderationIndexStart;
        considerationIndexTo = choiceIndex < Choices.Length - 1 ? Choices[choiceIndex + 1].ConsiderationIndexStart : (ushort)Considerations.Length;
        considerationIndex = considerationIndexFrom;
        modificationFactor = 1f - (1f / (considerationIndexTo - considerationIndexFrom));
        currentChoiceScore = choice.Weight + math.select(0f, choice.MomentumFactor, choice.ChoiceType == CurrentChoice);
    }

    private bool MoveToNextChoice()
    {
        UtilityScores.Add(currentChoiceScore);
        grandTotalScore += currentChoiceScore;

        choiceIndex++;
        if (choiceIndex == choiceIndexTo)
        {
            // there are no choices left to evaluate
            return false;
        }

        PrepareChoiceVariables();
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

    private void SelectChoiceWeightedRandom()
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