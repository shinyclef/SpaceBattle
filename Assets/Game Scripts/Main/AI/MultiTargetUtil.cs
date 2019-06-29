using System.Collections.Generic;

public static class MultiTargetUtil
{
    private readonly static Dictionary<ChoiceType, int> choiceTypeCounts;
    private readonly static HashSet<FactType> multiTargetFacts;

    static MultiTargetUtil()
    {
        choiceTypeCounts = new Dictionary<ChoiceType, int>()
        {
            { ChoiceType.FlyTowardsEnemyMulti, 5 }
        };

        multiTargetFacts = new HashSet<FactType>()
        {
            FactType.AngleFromTargetMulti,
            FactType.DistanceFromTargetMulti
        };
    }

    public static int GetChoiceTypeCount(ChoiceType type)
    {
        int count;
        if (!choiceTypeCounts.TryGetValue(type, out count))
        {
            count = 1;
        }

        return count;
    }

    public static bool IsMultiTargetFact(FactType type)
    {
        return multiTargetFacts.Contains(type);
    }
}