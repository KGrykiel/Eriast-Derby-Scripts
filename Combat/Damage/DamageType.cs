/// <summary>
/// Damage types for the D&D-style combat system.
/// Used for resistance/vulnerability calculations.
/// </summary>
public enum DamageType
{
    // Physical damage types
    Physical,       // Default, generic physical damage
    Bludgeoning,    // Hammers, rams, collisions (can split from Physical later)
    Piercing,       // Crossbows, spears, ballistas
    Slashing,       // Blades, saw weapons
    
    // Elemental damage types
    Fire,           // Flamethrowers, explosions
    Cold,           // Cryo weapons, ice magic
    Lightning,      // Tesla coils, shock weapons
    Acid,           // Chemical sprayers
    
    // Special damage types
    Force,          // Pure magical/mechanical force
    Psychic,        // Mind attacks (rare)
    Necrotic,       // Life-draining
    Radiant,        // Holy/light damage
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
