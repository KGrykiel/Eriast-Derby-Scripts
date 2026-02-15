using UnityEngine;

/// <summary>Central dice roller. The only place Random.Range is called</summary>
public static class RollUtility
{
    public static int RollDie(int dieSize)
    {
        return Random.Range(1, dieSize + 1);
    }

    public static int RollD20()
    {
        return Random.Range(1, 21);
    }

    /// <summary>
    /// D100 for initiative instead of typical D20 because of how many vehicles there will be.
    /// </summary>
    public static int RollInitiative()
    {
        return Random.Range(1, 101);
    }

    public static int RollDice(int diceCount, int dieSize)
    {
        if (diceCount <= 0 || dieSize <= 0) return 0;

        int total = 0;
        for (int i = 0; i < diceCount; i++)
        {
            total += Random.Range(1, dieSize + 1);
        }
        return total;
    }

    public static int[] RollDiceIndividual(int diceCount, int dieSize)
    {
        if (diceCount <= 0 || dieSize <= 0) return System.Array.Empty<int>();

        int[] results = new int[diceCount];
        for (int i = 0; i < diceCount; i++)
        {
            results[i] = Random.Range(1, dieSize + 1);
        }
        return results;
    }
}
