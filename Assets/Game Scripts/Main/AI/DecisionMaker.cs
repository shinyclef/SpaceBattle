using Unity.Entities;

public static class UtilityAi
{
    public static int SelectChoice(float totalScore, float r, ref DynamicBuffer<UtilityScoreBuf> choices)
    {
        float max = 0;
        
        for (int i = 0; i < choices.Length; i++)
        {
            max += choices[i].Value / totalScore;
            if (r < max)
            {
                return i;
            }
        }

        //Logger.Log($"Heads up, I'm retruning default choice! Max was {max}.");
        return 0;
    }
}