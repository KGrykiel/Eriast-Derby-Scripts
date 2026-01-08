/// <summary>
/// Defines how a skill's damage interacts with the weapon's base damage.
/// Allows flexible skill design - some scale with weapon, some don't.
/// </summary>
public enum SkillDamageMode
{
    /// <summary>
    /// Skill uses only its own dice, ignores weapon.
    /// Use for: Spells, utility abilities, weapon-independent attacks.
    /// DEFAULT for backward compatibility.
    /// </summary>
    SkillOnly,
    
    /// <summary>
    /// Skill uses only the weapon's dice.
    /// Use for: Basic attacks, standard shots.
    /// </summary>
    WeaponOnly,
    
    /// <summary>
    /// Skill adds its dice to weapon's dice.
    /// Use for: Power attacks, enhanced shots.
    /// Example: Weapon 1d8 + Skill 1d6 = 1d8+1d6
    /// </summary>
    WeaponPlusSkill,
    
    /// <summary>
    /// Skill multiplies the weapon's dice count.
    /// Use for: Critical hits, sneak attacks.
    /// Example: Weapon 1d8 × 2.0 = 2d8
    /// </summary>
    WeaponMultiplied
}
