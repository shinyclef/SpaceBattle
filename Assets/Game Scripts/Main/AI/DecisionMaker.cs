using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct DecisionMaker
{
    public NativeArray<Decision> Decisions;
    public NativeArray<Choice> Choices;
    public NativeArray<Consideration> Considerations;
    public DynamicBuffer<UtilityScoreBuf> UtilityScores;

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

    public DecisionMaker(ref NativeArray<Decision> decisions, ref NativeArray<Choice> choices,
        ref NativeArray<Consideration> considerations, ref DynamicBuffer<UtilityScoreBuf> utilityScores) : this()
    {
        Decisions = decisions;
        Choices = choices;
        Considerations = considerations;
        UtilityScores = utilityScores;
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
    }

    public bool EvaluateNextConsideration(float factValueRaw)
    {
        // evluate the considertion and multiply the result with the factValue
        Consideration consideration = Considerations[considerationIndex];

        // normalize input
        float factValue = consideration.GetNormalizedInput(factValueRaw);
        float score = math.clamp(consideration.Evaluate(factValue), 0f, 1f);
        float makeUpValue = (1 - score) * modificationFactor;
        //Logger.Log($"factValue: {factValue} ({factValueRaw}), score: {score}, makeUpValue: {makeUpValue}");
        score += makeUpValue * score;
        currentChoiceScore *= score;
        
        // if this is the last last consideration of the choice, or we're below minRequiredChoiceScore, move on the next choice.
        if (currentChoiceScore < minRequiredChoiceScore)
        {
            //Logger.Log($"Score cutooff. currentChoiceScore: {currentChoiceScore}, minRequiredChoiceScore: {minRequiredChoiceScore}");
            currentChoiceScore = 0f;
            considerationIndex = considerationIndexTo; // move to the next choice by saying we just finished the final choice
        }

        considerationIndex++;
        if (considerationIndex == considerationIndexTo)
        {
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
        currentChoiceScore = choice.Weight + choice.MomentumFactor;
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