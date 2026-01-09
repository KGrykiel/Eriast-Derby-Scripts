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
