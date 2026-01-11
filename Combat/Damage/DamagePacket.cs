using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Represents a packet of damage being dealt.
/// Flows through the damage pipeline for modification before application.
/// This is DATA - actual resolution happens in DamageResolver.
/// 
/// Source Tracking:
/// - attacker: The Entity dealing damage (for reflection, kill credit). Can be null for environmental/DoT.
/// - causalSource: What caused the damage (Skill, StatusEffect, Stage, etc.). For logging. Always set.
/// </summary>
[System.Serializable]
public struct DamagePacket
{
    /// <summary>
    /// The raw damage amount (after dice rolls, before resistances).
    /// </summary>
    public int amount;
    
    /// <summary>
    /// The type of damage being dealt.
    /// </summary>
    public DamageType type;
    
    /// <summary>
    /// The entity dealing the damage. Can be NULL for environmental/DoT damage.
    /// Used for: damage reflection, kill credit tracking.
    /// </summary>
    public Entity attacker;
    
    /// <summary>
    /// What caused this damage (Skill, StatusEffect, Stage, EventCard, etc.).
    /// Used for logging ("Destroyed by Fireball", "Destroyed by Burning").
    /// Should always be set for proper death logging.
    /// </summary>
    public UnityEngine.Object causalSource;
    
    /// <summary>
    /// What category of damage this is.
    /// </summary>
    public DamageSource sourceType;
    
    /// <summary>
    /// If true, this damage bypasses resistance (but not immunity).
    /// </summary>
    public bool ignoresResistance;
    
    /// <summary>
    /// If true, this damage bypasses shield components.
    /// </summary>
    public bool ignoresShields;
    
    /// <summary>
    /// Armor penetration value - reduces effective AC for this damage.
    /// (Used in hit calculation, not damage calculation)
    /// </summary>
    public int armorPenetration;
    
    /// <summary>
    /// Was this damage from a critical hit?
    /// </summary>
    public bool isCritical;
    
    /// <summary>
    /// Get display name for the causal source (for logging).
    /// </summary>
    public string CausalSourceName => causalSource != null ? causalSource.name : "Unknown";
    
    /// <summary>
    /// Get display name for the attacker (for logging).
    /// Returns "Environment" if no attacker.
    /// </summary>
    public string AttackerName => attacker != null ? attacker.GetDisplayName() : "Environment";
    
    // ==================== FACTORY METHODS ====================
    
    /// <summary>
    /// Create damage from an attacker entity (skill attack, weapon attack).
    /// Has attacker for kill credit, damage reflection.
    /// </summary>
    public static DamagePacket FromAttacker(
        int amount,
        DamageType type,
        Entity attacker,
        UnityEngine.Object causalSource,
        DamageSource sourceType = DamageSource.Ability)
    {
        return new DamagePacket
        {
            amount = amount,
            type = type,
            attacker = attacker,
            causalSource = causalSource,
            sourceType = sourceType,
            ignoresResistance = false,
            ignoresShields = false,
            armorPenetration = 0,
            isCritical = false
        };
    }
    
    /// <summary>
    /// Create damage from a weapon attack.
    /// </summary>
    public static DamagePacket FromWeapon(
        int amount,
        DamageType type,
        Entity attacker,
        UnityEngine.Object causalSource,
        bool isCritical = false)
    {
        return new DamagePacket
        {
            amount = amount,
            type = type,
            attacker = attacker,
            causalSource = causalSource,
            sourceType = DamageSource.Weapon,
            ignoresResistance = false,
            ignoresShields = false,
            armorPenetration = 0,
            isCritical = isCritical
        };
    }
    
    /// <summary>
    /// Create environmental/DoT damage (no attacker entity).
    /// Used for: status effect DoT, stage hazards, traps.
    /// </summary>
    public static DamagePacket Environmental(
        int amount,
        DamageType type,
        UnityEngine.Object causalSource,
        DamageSource sourceType = DamageSource.Effect)
    {
        return new DamagePacket
        {
            amount = amount,
            type = type,
            attacker = null,  // No attacker
            causalSource = causalSource,
            sourceType = sourceType,
            ignoresResistance = false,
            ignoresShields = false,
            armorPenetration = 0,
            isCritical = false
        };
    }
    
    /// <summary>
    /// Legacy factory - prefer FromAttacker or Environmental.
    /// </summary>
    [Obsolete("Use FromAttacker or Environmental instead for clarity")]
    public static DamagePacket Create(int amount, DamageType type, Entity source = null)
    {
        return new DamagePacket
        {
            amount = amount,
            type = type,
            attacker = source,
            causalSource = null,
            sourceType = DamageSource.Ability,
            ignoresResistance = false,
            ignoresShields = false,
            armorPenetration = 0,
            isCritical = false
        };
    }
    
    public override string ToString()
    {
        string critText = isCritical ? " (CRIT)" : "";
        string sourceText = causalSource != null ? $" from {CausalSourceName}" : "";
        return $"{amount} {type} damage{critText}{sourceText}";
    }
}

/// <summary>
/// What caused the damage - for logging and special handling.
/// </summary>
public enum DamageSource
{
    Weapon,         // From a weapon component attack
    Ability,        // From a skill/spell (non-weapon)
    Environment,    // Stage hazards, traps
    Effect,         // Damage over time, ongoing status effects
    Collision       // Vehicle collisions
}

/// <summary>
/// Resistance level to a damage type.
/// </summary>
public enum ResistanceLevel
{
    /// <summary>Takes double damage (×2)</summary>
    Vulnerable,
    
    /// <summary>Takes normal damage (×1)</summary>
    Normal,
    
    /// <summary>Takes half damage (×0.5, rounded down)</summary>
    Resistant,
    
    /// <summary>Takes no damage (×0)</summary>
    Immune
}

/// <summary>
/// A single resistance entry for an entity.
/// </summary>
[System.Serializable]
public struct DamageResistance
{
    public DamageType type;
    public ResistanceLevel level;
    
    public DamageResistance(DamageType type, ResistanceLevel level)
    {
        this.type = type;
        this.level = level;
    }
}
