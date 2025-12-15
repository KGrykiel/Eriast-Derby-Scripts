using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Central damage processing system.
/// All damage flows through here for consistent handling of:
/// - Resistances/Vulnerabilities
/// - Shields (future)
/// - Hardness (future)
/// - Logging
/// </summary>
public static class DamageResolver
{
    /// <summary>
    /// Process a damage packet against a target entity.
    /// Applies all modifiers and returns the final damage dealt.
    /// Does NOT apply damage to target - caller must do that.
    /// </summary>
    /// <param name="packet">The damage packet to process</param>
    /// <param name="target">The target entity</param>
    /// <returns>Final damage amount after all modifiers</returns>
    public static int ResolveDamage(DamagePacket packet, Entity target)
    {
        if (target == null) return 0;
        if (packet.amount <= 0) return 0;

        int damage = packet.amount;

        // Step 1: Apply shields (future - for now, skip)
        // damage = ApplyShields(damage, packet, target);

        // Step 2: Apply resistances/vulnerabilities
        if (!packet.ignoresResistance)
        {
            damage = ApplyResistances(damage, packet.type, target);
        }

        // Step 3: Apply hardness (future - flat damage reduction for objects)
        // damage = ApplyHardness(damage, target);

        // Step 4: Ensure minimum damage (0)
        damage = Mathf.Max(0, damage);

        // Log the resolution (debug level)
        LogDamageResolution(packet, target, damage);

        return damage;
    }

    /// <summary>
    /// Apply resistance/vulnerability modifiers to damage.
    /// </summary>
    private static int ApplyResistances(int damage, DamageType type, Entity target)
    {
        ResistanceLevel resistance = GetResistance(target, type);

        switch (resistance)
        {
            case ResistanceLevel.Vulnerable:
                return damage * 2;

            case ResistanceLevel.Resistant:
                return damage / 2;  // Rounded down (integer division)

            case ResistanceLevel.Immune:
                return 0;

            case ResistanceLevel.Normal:
            default:
                return damage;
        }
    }

    /// <summary>
    /// Get an entity's resistance level to a damage type.
    /// </summary>
    private static ResistanceLevel GetResistance(Entity target, DamageType type)
    {
        // Check if entity has resistance data
        // For now, return Normal - entities will implement GetResistance() method
        // This will be expanded when Entity.cs is updated

        // Future: target.GetResistance(type)
        return ResistanceLevel.Normal;
    }

    /// <summary>
    /// Log damage resolution for debugging.
    /// </summary>
    private static void LogDamageResolution(DamagePacket packet, Entity target, int finalDamage)
    {
        // Only log if damage was modified or for debug purposes
        if (packet.amount != finalDamage)
        {
            string targetName = target?.GetDisplayName() ?? "Unknown";
            Debug.Log($"[DamageResolver] {packet.amount} {packet.type} → {finalDamage} to {targetName}");
        }
    }

    // ==================== FUTURE EXPANSION ====================

    /// <summary>
    /// Apply shield absorption (future implementation).
    /// Shields absorb damage before it reaches HP.
    /// </summary>
    // private static int ApplyShields(int damage, DamagePacket packet, Entity target)
    // {
    //     if (packet.ignoresShields) return damage;
    //     // Check for shield components on target's vehicle
    //     // Absorb damage up to shield capacity
    //     return damage;
    // }

    /// <summary>
    /// Apply hardness (future implementation).
    /// Hardness is flat damage reduction for objects/constructs.
    /// </summary>
    // private static int ApplyHardness(int damage, Entity target)
    // {
    //     int hardness = target.GetHardness();
    //     return Mathf.Max(0, damage - hardness);
    // }
}
