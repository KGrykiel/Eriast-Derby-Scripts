using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Represents a packet of damage being dealt.
/// Flows through the damage pipeline for modification before application.
/// This is DATA - actual resolution happens in DamageResolver.
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
    /// The entity dealing the damage (for logging/effects).
    /// </summary>
    public Entity source;
    
    /// <summary>
    /// What caused this damage.
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
    /// Create a simple damage packet with just amount and type.
    /// </summary>
    public static DamagePacket Create(int amount, DamageType type, Entity source = null)
    {
        return new DamagePacket
        {
            amount = amount,
            type = type,
            source = source,
            sourceType = DamageSource.Ability,
            ignoresResistance = false,
            ignoresShields = false,
            armorPenetration = 0,
            isCritical = false
        };
    }
    
    /// <summary>
    /// Create a damage packet from a weapon attack.
    /// </summary>
    public static DamagePacket FromWeapon(int amount, DamageType type, Entity source, bool isCritical = false)
    {
        return new DamagePacket
        {
            amount = amount,
            type = type,
            source = source,
            sourceType = DamageSource.Weapon,
            ignoresResistance = false,
            ignoresShields = false,
            armorPenetration = 0,
            isCritical = isCritical
        };
    }
    
    public override string ToString()
    {
        string critText = isCritical ? " (CRIT)" : "";
        return $"{amount} {type} damage{critText}";
    }
}

/// <summary>
/// What caused the damage - for logging and special handling.
/// </summary>
public enum DamageSource
{
    Weapon,         // From a weapon component
    Ability,        // From a skill/spell (non-weapon)
    Environment,    // Stage hazards, collisions
    Effect,         // Damage over time, ongoing effects
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
