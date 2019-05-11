using Unity.Entities;
using Unity.Mathematics;

public static class UtilityAi
{
    public static int SelectChoice(float totalScore, ref Random rand, ref DynamicBuffer<UtilityScoreBuf> choices)
    {
        float max = 0;
        
        for (int i = 0; i < choices.Length; i++)
        {
            max += choices[i].Value / totalScore;
            if (rand.NextFloat() < max)
            {
                return i;
            }
        }

        // Logger.Log($"Heads up, I'm retruning default choice! Max was {max}.");
        return 0;
    }
}