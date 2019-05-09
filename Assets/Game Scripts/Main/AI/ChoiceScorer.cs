using System.Collections.Generic;

// COMPONENT
public struct Decision
{
    // What should I eat?

    public List<Choice> Choices; // Dynamcic Buffer 1
    public List<Consideration> Considerations; // Dynamcic Buffer 2

    public void MakeDecision()
    {
        EvaluateChoices();
    }

    public void EvaluateChoices()
    {
        float grandTotalScore = 0f;
        float minRequiredScore = 0f;
        for (int i = 0; i < Choices.Count; i++)
        {
            Choice choice = Choices[i];
            float totalScore = choice.Weight + choice.MomentumFactor;
            int considerationCount = choice.Considerations.Count;
            float modificationFactor = 1f - (1f / considerationCount);
            for (int j = 0; j < choice.Considerations.Count; j++)
            {
                if (totalScore <= minRequiredScore)
                {
                    totalScore = 0f;
                    break;
                }

                // get the fact
                float factValue = 0.5f;

                // evluate the considertion and multiply the result with the factValue
                float score = choice.Considerations[j].Evaluate(factValue);
                float makeUpValue = (1 - score) * modificationFactor;
                score += makeUpValue * score;
                totalScore *= score;
                
            }

            choice.Score = totalScore;
            grandTotalScore += choice.Score;
        }
    }

    public void SelectChoice()
    {
        // random weighted selection
    }
}

public struct Choice
{
    public List<Consideration> Considerations;
    public float Weight;
    public float MomentumFactor;
    public float Score;

    public void Evaluate()
    {
        foreach (Consideration c in Considerations)
        {
            // get the fact from some kind of fact lookup
            float factValue = 0.5f;

            // evaluate it
            Score = c.Evaluate(factValue);
        }
    }
}
