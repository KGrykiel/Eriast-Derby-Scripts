/// <summary>
/// Defines how damage calculation interacts with weapons.
/// Allows flexible design - some damage scales with weapon, some doesn't.
/// Used by skills, event cards, and other damage sources.
/// </summary>
public enum DamageMode
{
    /// <summary>
    /// Uses only the base dice defined in the formula, ignores weapon.
    /// Use for: Spells, environmental damage, event cards, weapon-independent effects.
    /// DEFAULT for backward compatibility.
    /// </summary>
    BaseOnly,
    
    /// <summary>
    /// Uses only the weapon's dice.
    /// Use for: Basic attacks, standard shots.
    /// </summary>
    WeaponOnly,
    
    /// <summary>
    /// Adds formula's base dice to weapon's dice.
    /// Use for: Power attacks, enhanced shots, weapon-scaling skills.
    /// Example: Weapon 1d8 + Formula 1d6 = 1d8+1d6
    /// </summary>
    WeaponPlusBase
}
