using System;
using Unity.Mathematics;
using UnityEngine.Assertions;

[Serializable]
public class UtilityAiDto : IEquatable<UtilityAiDto>
{
    public DecisionDto[] Decisions;

    public bool Equals(UtilityAiDto other)
    {
        if (Decisions.Length != other.Decisions.Length)
        {
            return false;
        }

        for (int i = 0; i < Decisions.Length; i++)
        {
            if (!Decisions[i].Equals(other.Decisions[i]))
            {
                return false;
            }
        }

        return true;
    }

    public UtilityAiDto Clone()
    {
        var decisions = new DecisionDto[Decisions.Length];
        for (int i = 0; i < Decisions.Length; i++)
        {
            decisions[i] = Decisions[i].Clone();
        }

        return new UtilityAiDto
        {
            Decisions = decisions
        };
    }

    public void CopyValuesFrom(UtilityAiDto other)
    {
        if (Decisions.Length != other.Decisions.Length)
        {
            Assert.IsTrue(false, "Cannot copy UtilityAiDto as Decisions.Length != other.Decisions.Length.");
            return;
        }

        for (int i = 0; i < Decisions.Length; i++)
        {
            Decisions[i].CopyValuesFrom(other.Decisions[i]);
        }
    }
}

[Serializable]
public class DecisionDto : IEquatable<DecisionDto>
{
    public string DecisionType;
    public float MinimumRequiredOfBest;
    public ChoiceDto[] Choices;

    public bool Equals(DecisionDto other)
    {
        if (DecisionType != other.DecisionType ||
            MinimumRequiredOfBest != other.MinimumRequiredOfBest ||
            Choices.Length != other.Choices.Length)
        {
            return false;
        }

        for (int i = 0; i < Choices.Length; i++)
        {
            if (!Choices[i].Equals(other.Choices[i]))
            {
                return false;
            }
        }

        return true;
    }

    public DecisionDto Clone()
    {
        var choices = new ChoiceDto[Choices.Length];
        for (int i = 0; i < Choices.Length; i++)
        {
            choices[i] = Choices[i].Clone();
        }

        return new DecisionDto
        {
            DecisionType = DecisionType,
            MinimumRequiredOfBest = MinimumRequiredOfBest,
            Choices = choices
        };
    }

    public void CopyValuesFrom(DecisionDto other)
    {
        if (Choices.Length != other.Choices.Length)
        {
            Assert.IsTrue(false, "Cannot copy DecisionDto as Choices.Length != other.Choices.Length.");
            return;
        }

        DecisionType = other.DecisionType;
        MinimumRequiredOfBest = other.MinimumRequiredOfBest;
        for (int i = 0; i < Choices.Length; i++)
        {
            Choices[i].CopyValuesFrom(other.Choices[i]);
        }
    }

    public Decision ToDecision(ushort choiceIndexStart)
    {
        return new Decision
        {
            DecisionType = (DecisionType)Enum.Parse(typeof(DecisionType), DecisionType),
            ChoiceIndexStart = choiceIndexStart,
            MinimumRequiredOfBest = new half(MinimumRequiredOfBest)
        };
    }
}

[Serializable]
public class ChoiceDto : IEquatable<ChoiceDto>
{
    public string ChoiceType;
    public float Weight;
    public float Momentum;
    public ConsiderationDto[] Considerations;

    public int TargetCount { get { return MultiTargetUtil.GetChoiceTypeCount((ChoiceType)Enum.Parse(typeof(ChoiceType), ChoiceType)); } }
    public ChoiceType ChoiceTypeEnum { get { return (ChoiceType)Enum.Parse(typeof(ChoiceType), ChoiceType); } }

    public bool Equals(ChoiceDto other)
    {
        if (ChoiceType != other.ChoiceType ||
            Weight != other.Weight ||
            Momentum != other.Momentum ||
            Considerations.Length != other.Considerations.Length)
        {
            return false;
        }

        for (int i = 0; i < Considerations.Length; i++)
        {
            if (!Considerations[i].Equals(other.Considerations[i]))
            {
                return false;
            }
        }

        return true;
    }

    public ChoiceDto Clone()
    {
        var considerations = new ConsiderationDto[Considerations.Length];
        for (int i = 0; i < Considerations.Length; i++)
        {
            considerations[i] = Considerations[i].Clone();
        }

        return new ChoiceDto
        {
            ChoiceType = ChoiceType,
            Weight = Weight,
            Momentum = Momentum,
            Considerations = considerations
        };
    }

    public void CopyValuesFrom(ChoiceDto other)
    {
        if (Considerations.Length != other.Considerations.Length)
        {
            Assert.IsTrue(false, "Cannot copy ChoiceDto as Considerations.Length != other.Considerations.Length.");
            return;
        }

        ChoiceType = other.ChoiceType;
        Weight = other.Weight;
        Momentum = other.Momentum;
        for (int i = 0; i < Considerations.Length; i++)
        {
            Considerations[i].CopyValuesFrom(other.Considerations[i]);
        }
    }

    public Choice ToChoice(ushort considerationIndexStart)
    {
        return new Choice
        {
            ChoiceType = ChoiceTypeEnum,
            TargetCount = (ushort)TargetCount,
            ConsiderationIndexStart = considerationIndexStart,
            Weight = Weight,
            MomentumFactor = Momentum
        };
    }
}

[Serializable]
public class ConsiderationDto : IEquatable<ConsiderationDto>
{
    public string FactType;
    public string GraphType;
    public float Slope;
    public float Exp;
    public float XShift;
    public float YShift;
    public float InputMin;
    public float InputMax;

    public bool IsMultiTarget { get { return FactType.EndsWith("Multi"); } }

    public FactType FactTypeEnum { get { return (FactType)Enum.Parse(typeof(FactType), FactType); } }

    public static ConsiderationDto GetDefault()
    {
        return new ConsiderationDto
        {
            FactType = "Constant",
            GraphType = "Constant",
            Slope = 0,
            Exp = 0,
            XShift = 0,
            YShift = 0.5f,
            InputMin = 0,
            InputMax = 0
        };
    }

    public bool Equals(ConsiderationDto other)
    {
        return FactType == other.FactType &&
            GraphType == other.GraphType &&
            Slope == other.Slope &&
            Exp == other.Exp &&
            XShift == other.XShift &&
            YShift == other.YShift &&
            InputMin == other.InputMin &&
            InputMax == other.InputMax;
    }

    public ConsiderationDto Clone()
    {
        return new ConsiderationDto
        {
            FactType = FactType,
            GraphType = GraphType,
            Slope = Slope,
            Exp = Exp,
            XShift = XShift,
            YShift = YShift,
            InputMin = InputMin,
            InputMax = InputMax
        };
    }

    public void CopyValuesFrom(ConsiderationDto other)
    {
        FactType = other.FactType;
        GraphType = other.GraphType;
        Slope = other.Slope;
        Exp = other.Exp;
        XShift = other.XShift;
        YShift = other.YShift;
        InputMin = other.InputMin;
        InputMax = other.InputMax;
    }

    public Consideration ToConsideration()
    {
        return new Consideration
        {
            FactType = (FactType)Enum.Parse(typeof(FactType), FactType),
            GraphType = (GraphType)Enum.Parse(typeof(GraphType), GraphType),
            Slope = Slope,
            Exp = Exp,
            XShift = new half(XShift),
            YShift = new half(YShift),
            InputMin = InputMin,
            InputMax = InputMax
        };
    }
}