using Random = Unity.Mathematics.Random;

/// <summary>
/// This class is not thread safe. It is intended to be used from the main thread only.
/// </summary>
public static class Rand
{
    private static System.Random sysRand = new System.Random();

    public static Random New()
    {
        return new Random((uint)(sysRand.Next(1, int.MaxValue)));
    }
}