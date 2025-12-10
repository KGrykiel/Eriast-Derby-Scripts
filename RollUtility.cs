using UnityEngine;
public static class RollUtility
{
    /// <summary>
    /// Roll to hit an entity. Vehicle parameter is kept for backward compatibility.
    /// </summary>
    public static bool RollToHit(Vehicle user, Entity target, RollType rollType, int toHitBonus = 0, string contextName = null)
    {
        // Get target vehicle from entity (if it's a component)
        Vehicle vehicleTarget = GetParentVehicle(target);
        if (vehicleTarget == null)
            return true; // Always hit non-vehicle entities
        
        // Get user vehicle name
        string userName = user?.vehicleName ?? "Unknown";

        int roll = UnityEngine.Random.Range(1, 21) + toHitBonus;
        int targetValue = 0;
        switch (rollType)
        {
            case RollType.ArmorClass:
                // Use target entity's AC directly if targeting a component
                if (target is VehicleComponent)
                {
                    targetValue = target.GetArmorClass();
                }
                else
                {
                    targetValue = Mathf.RoundToInt(vehicleTarget.GetAttribute(Attribute.ArmorClass));
                }
                break;
            case RollType.MagicResistance:
                targetValue = Mathf.RoundToInt(vehicleTarget.GetAttribute(Attribute.MagicResistance));
                break;
        }
        string context = contextName ?? "effect";
        string targetName = vehicleTarget.vehicleName;
        if (target is VehicleComponent comp)
        {
            targetName = $"{vehicleTarget.vehicleName}'s {comp.name}";
        }
        SimulationLogger.LogEvent($"{userName} rolls {roll} to hit {targetName} (target value: {targetValue}) with {context}.");
        return roll >= targetValue;
    }
    
    /// <summary>
    /// Get parent vehicle from an entity (if it's a VehicleComponent).
    /// </summary>
    private static Vehicle GetParentVehicle(Entity entity)
    {
        if (entity is VehicleComponent component)
        {
            return component.ParentVehicle;
        }
        return null;
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
