using System;

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
}

[Serializable]
public class ChoiceDto
{
    public ChoiceType ChoiceType;
    public string ChoiceName;
    public float Weight;
    public float Momentum;
    public ConsiderationDto[] Considerations;
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
}