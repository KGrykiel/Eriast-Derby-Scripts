using UnityEngine;
public static class RollUtility
{
    public static bool RollToHit(Vehicle user, Entity target, RollType rollType, int toHitBonus = 0, string contextName = null)
    {
        var vehicleTarget = target as Vehicle;
        if (vehicleTarget == null)
            return true; // Always hit non-vehicles

        int roll = UnityEngine.Random.Range(1, 21) + toHitBonus;
        int targetValue = 0;
        switch (rollType)
        {
            case RollType.ArmorClass:
                targetValue = Mathf.RoundToInt(vehicleTarget.GetAttribute(Attribute.ArmorClass));
                break;
            case RollType.MagicResistance:
                targetValue = Mathf.RoundToInt(vehicleTarget.GetAttribute(Attribute.MagicResistance));
                break;
        }
        string context = contextName ?? "effect";
        SimulationLogger.LogEvent($"{user.vehicleName} rolls {roll} to hit {vehicleTarget.vehicleName} (target value: {targetValue}) with {context}.");
        return roll >= targetValue;
    }
    /// <summary>
    /// Rolls a number of dice and adds a bonus, for damage rolls.
    /// </summary>
    public static int RollDamage(int diceCount, int dieSize, int bonus = 0)
    {
        int total = bonus;
        for (int i = 0; i < diceCount; i++)
            total += Random.Range(1, dieSize + 1);
        SimulationLogger.LogEvent($"Rolling damage: {diceCount}d{dieSize} + {bonus} = {total}");
        return total;
    }

    /// <summary>
    /// Performs a skill check: rolls d20 + bonus and compares to difficulty.
    /// Returns (success, roll, bonus, total).
    /// </summary>
    public static (bool success, int roll, int bonus, int total) SkillCheck(int bonus, int difficulty)
    {
        int roll = UnityEngine.Random.Range(1, 21);
        int total = roll + bonus;
        bool success = total >= difficulty;
        return (success, roll, bonus, total);
    }

    // will add more here (e.g.,saving throws, etc.)
}

public enum RollType
{
    None,           // Always hits
    ArmorClass,     // Uses ArmorClass
    MagicResistance // Uses MagicResistance
    // will add more later
}
