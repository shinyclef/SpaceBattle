using NUnit.Framework;
using UnityEngine;

namespace Tests
{
    public class UtilityAiJson : AssertionHelper
    {
        [Test]
        public void ExampleTestSimplePasses()
        {
            var dto = new UtilityAiDto
            {
                Decisions = new DecisionDto[]
                {
                    new DecisionDto
                    {
                        DecisionType = DecisionType.CombatMovement,
                        MinimumRequiredOfBest = 0.1f,
                        Choices = new ChoiceDto[]
                        {
                            new ChoiceDto
                            {
                                ChoiceType = ChoiceType.FlyTowardEnemy,
                                Weight = 1f,
                                Momentum = 1f,
                                Considerations = new ConsiderationDto[]
                                {
                                    new ConsiderationDto
                                    {
                                        FactType = FactType.DistanceFromTarget,
                                        GraphType = GraphType.Exponential,
                                        Slope = -0.6f,
                                        Exp = 6f,
                                        XShift = 1f,
                                        YShift = 0.8f,
                                        InputMin = 0f,
                                        InputMax = 20f
                                    },
                                    new ConsiderationDto
                                    {
                                        FactType = FactType.AngleFromTarget,
                                        GraphType = GraphType.Exponential,
                                        Slope = 0.6f,
                                        Exp = 6f,
                                        XShift = 1f,
                                        YShift = 0.2f,
                                        InputMin = 0f,
                                        InputMax = 180f
                                    }
                                }
                            },
                            new ChoiceDto
                            {
                                ChoiceType = ChoiceType.FlyAwayFromEnemy,
                                Weight = 1f,
                                Momentum = 1f,
                                Considerations = new ConsiderationDto[]
                                {
                                    new ConsiderationDto
                                    {
                                        FactType = FactType.DistanceFromTarget,
                                        GraphType = GraphType.Exponential,
                                        Slope = 0.6f,
                                        Exp = 6f,
                                        XShift = 1f,
                                        YShift = 0.2f,
                                        InputMin = 5f,
                                        InputMax = 20f
                                    },
                                    new ConsiderationDto
                                    {
                                        FactType = FactType.AngleFromTarget,
                                        GraphType = GraphType.Exponential,
                                        Slope = -0.6f,
                                        Exp = 6f,
                                        XShift = 1f,
                                        YShift = 0.8f,
                                        InputMin = 0f,
                                        InputMax = 180f
                                    }
                                }
                            }
                        }
                    }
                }
            };

            string json = JsonUtility.ToJson(dto);
        }
    }
}
