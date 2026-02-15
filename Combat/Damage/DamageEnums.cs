namespace Assets.Scripts.Combat.Damage
{
    /// <summary>Standard D&D damage type spread.</summary>
    public enum DamageType
    {
        Physical, //might remove that one, I think the subtypes cover all bases already
        Bludgeoning,
        Piercing,
        Slashing,

        Fire,
        Cold,
        Lightning,
        Acid,

        Force,
        Psychic,
        Necrotic,
        Radiant,
    }

    /// <summary>
    /// What caused the damage - for logging and special handling.
    /// </summary>
    public enum DamageSource
    {
        Weapon,         // From a weapon component attack
        Ability,        // From a character skill/spell (non-weapon)
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
}
