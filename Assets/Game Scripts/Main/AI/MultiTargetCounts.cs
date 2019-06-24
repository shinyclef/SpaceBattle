using System.Collections.Generic;

public static class MultiTargetCounts
{
    public static Dictionary<string, int> choiceTypeCounts;
    public static Dictionary<string, int> factTypeCounts;

    static MultiTargetCounts()
    {
        choiceTypeCounts = new Dictionary<string, int>()
        {
            { ChoiceType.FlyTowardsEnemyMulti.ToString(), 5 }
        };

        factTypeCounts = new Dictionary<string, int>()
        {
            { FactType.DistanceFromTargetMulti.ToString(), 5 },
            { FactType.AngleFromTargetMulti.ToString(), 5 }
        };
    }

    public static int GetChoiceTypeCount(string type)
    {
        int count;
        if (!choiceTypeCounts.TryGetValue(type, out count))
        {
            count = 1;
        }

        return count;
    }

    public static int GetFactTypeCount(string type)
    {
        int count;
        if (!factTypeCounts.TryGetValue(type, out count))
        {
            count = 1;
        }

        return count;
    }
}