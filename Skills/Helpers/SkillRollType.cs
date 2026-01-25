/// <summary>
/// Defines the type of roll required for a skill to succeed.
/// Determines both the mechanics and target number calculation.
/// </summary>
public enum SkillRollType
{
    /// <summary>
    /// No roll required - skill automatically succeeds (buffs, heals, etc.)
    /// </summary>
    None,
    
    /// <summary>
    /// Attack roll - d20 + attack bonus vs target's AC
    /// Used for weapon attacks and offensive spells
    /// </summary>
    AttackRoll,
    
    /// <summary>
    /// Saving throw - target rolls d20 + save bonus vs skill's DC
    /// Used for effects that target can actively resist (poison, stunning, etc.)
    /// </summary>
    SavingThrow,
    
    /// <summary>
    /// Skill check - d20 + skill bonus vs DC
    /// Used for non-combat actions (lockpicking, hacking, stealth, etc.)
    /// </summary>
    SkillCheck,
    
    /// <summary>
    /// Opposed check - both user and target roll, highest wins
    /// Used for contested actions (grapple, hacking vs countermeasures, etc.)
    /// </summary>
    OpposedCheck
}

/// <summary>
/// Defines how precisely a skill can target components vs vehicles.
/// Controls UI component selection and effect routing.
/// </summary>
public enum TargetPrecision
{
    /// <summary>
    /// Vehicle-only targeting. Always hits chassis regardless of player selection.
    /// Used for: Area attacks, cannons, non-precise weapons.
    /// UI: No component selector shown.
    /// </summary>
    VehicleOnly,

    /// <summary>
    /// Automatic routing based on effect attributes. Player targets vehicle, system routes to appropriate component.
    /// Used for: Debuffs (Slow → Drive), buffs (Shield → Chassis), most abilities.
    /// UI: No component selector shown.
    /// </summary>
    Auto,

    /// <summary>
    /// Precise targeting. Player must select specific component.
    /// Used for: Sniper rifles, targeted abilities, surgical strikes.
    /// UI: Component selector shown.
    /// </summary>
    Precise
}
