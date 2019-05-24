using System;
using Unity.Mathematics;

[Serializable]
public class UtilityAiDto
{
    public DecisionDto[] Decisions;
}

[Serializable]
public class DecisionDto
{
    public DecisionType DecisionType;
    public float MinimumRequiredOfBest;
    public ChoiceDto[] Choices;

    public Decision ToDecision(ushort choiceIndexStart)
    {
        return new Decision
        {
            DecisionType = DecisionType,
            ChoiceIndexStart = choiceIndexStart,
            MinimumRequiredOfBest = new half(MinimumRequiredOfBest)
        };
    }
}

[Serializable]
public class ChoiceDto
{
    public ChoiceType ChoiceType;
    public string ChoiceName;
    public float Weight;
    public float Momentum;
    public ConsiderationDto[] Considerations;

    public Choice ToChoice(ushort considerationIndexStart)
    {
        return new Choice
        {
            ChoiceType = ChoiceType,
            ConsiderationIndexStart = considerationIndexStart,
            Weight = Weight,
            MomentumFactor = Momentum
        };
    }
}

[Serializable]
public class ConsiderationDto
{
    public FactType FactType;
    public GraphType GraphType;
    public float Slope;
    public float Exp;
    public float XShift;
    public float YShift;
    public float InputMin;
    public float InputMax;

    public Consideration ToConsideration()
    {
        return new Consideration
        {
            FactType = FactType,
            GraphType = GraphType,
            Slope = Slope,
            Exp = Exp,
            XShift = new half(XShift),
            YShift = new half(YShift),
            InputMin = InputMin,
            InputMax = InputMax
        };
    }
}