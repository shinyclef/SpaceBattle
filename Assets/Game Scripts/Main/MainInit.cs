using UnityEngine;
using UnityEngine.Assertions;

public class MainInit
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Initialise()
    {
        ValidateNearestEnemyUpdateTimes();
    }

    private static void ValidateNearestEnemyUpdateTimes()
    {
        Assert.IsTrue(NearestEnemyRequestSys.UpdateInterval > CombatAiSys.RefreshNearestEnemiesInterval,
            $"{nameof(NearestEnemyRequestSys.UpdateInterval)} must be greater than {nameof(CombatAiSys.RefreshNearestEnemiesInterval)} to prevent target resetting which leads to AI hiccups.");
    }
}