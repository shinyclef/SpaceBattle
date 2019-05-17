using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class UtilityAiJson : AssertionHelper
    {
        [Test]
        public void ExampleTestSimplePasses()
        {
            var dto = new UtilityAiDto
            {
                Choices = new ChoiceDto[]
                {
                    new ChoiceDto
                    {
                        ChoiceType = ChoiceType.FlyTowardEnemy,
                        Weight = 1f,
                        Momentum = 1f,
                        ConsiderationIndecies = new int[]
                        {
                            0,
                            1
                        }
                    },
                    new ChoiceDto
                    {
                        ChoiceType = ChoiceType.FlyAwayFromEnemy,
                        Weight = 1f,
                        Momentum = 1f,
                        ConsiderationIndecies = new int[]
                        {
                            2,
                            3
                        }
                    }
                },
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
                    },
                    new ConsiderationDto
                    {
                        FactType = FactType.DistanceFromTarget,
                        GraphType = GraphType.Exponential,
                        Slope = 0.6f,
                        Exp = 6f,
                        XShift = 1f,
                        YShift = 0.2f,
                        InputMin = 0f,
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
            };

            string json = JsonUtility.ToJson(dto);
        }
    }
}
