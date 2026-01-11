using UnityEngine;

/// <summary>
/// Central utility class for ALL dice rolling in the game.
/// This is THE ONLY place where Random.Range should be called for dice.
/// 
/// DESIGN NOTE: Bonuses are NOT added here. They are tracked separately in:
/// - DamageSourceEntry.bonus (for damage breakdowns)
/// - AttackModifier.value (for attack roll breakdowns)
/// This allows tooltips to show dice vs bonus separately.
/// 
/// Used by:
/// - AttackCalculator (d20 rolls)
/// - DamageCalculator (damage dice)
/// - DamageFormula (skill/weapon damage)
/// </summary>
public static class RollUtility
{
    /// <summary>
    /// Roll a single die of the specified size (e.g., d6, d8, d20).
    /// </summary>
    public static int RollDie(int dieSize)
    {
        return Random.Range(1, dieSize + 1);
    }
    
    /// <summary>
    /// Roll a d20. Convenience method for attack rolls and checks.
    /// </summary>
    public static int RollD20()
    {
        return Random.Range(1, 21);
    }
    
    /// <summary>
    /// Roll multiple dice and return the sum.
    /// Example: RollDice(2, 6) rolls 2d6 and returns the total.
    /// 
    /// NOTE: Bonuses should be tracked separately via DamageSourceEntry or AttackModifier,
    /// not added to the roll. This enables proper breakdown display in tooltips.
    /// </summary>
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
    
    /// <summary>
    /// Roll multiple dice and return individual results.
    /// Useful for showing each die in tooltips or for special mechanics (e.g., exploding dice).
    /// </summary>
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
