public enum SkillRollType
{
    /// <summary>
    /// No roll required - skill automatically succeeds (buffs, heals, etc.)
    /// </summary>
    None,
    
    /// <summary>
    /// Attack roll - d20 + bonuses vs target's AC
    /// Used for weapon attacks and offensive skills/spells
    /// </summary>
    AttackRoll,
    
    /// <summary>
    /// Saving throw - TARGET rolls d20 + save bonus vs skill's DC
    /// Used for effects that target can actively resist (poison, stunning, etc.)
    /// </summary>
    SavingThrow,

    /// <summary>
    /// Skill check - d20 + skill bonus vs DC
    /// Used for event cards or skills that require an active check (piloting through hazards, performing stunts, etc.)
    /// </summary>
    SkillCheck,
    
    /// <summary>
    /// Opposed check - both user and target roll, highest wins
    /// Used for contested actions (grapple, hacking vs countermeasures, etc.)
    /// </summary>
    OpposedCheck
}

/// <summary>
/// Determines the UI necessary to select targets for a skill, and how the skill's effects are applied to those targets.
/// </summary>
public enum TargetingMode
{
    /// <summary>
    /// No targeting required - self-cast skill (buffs, repairs to own chassis).
    /// UI: No selection screen shown.
    /// </summary>
    Self,
    
    /// <summary>
    /// Player selects a component on their own vehicle (targeted repairs/buffs).
    /// UI: Shows source vehicle's component selector.
    /// </summary>
    SourceComponent,
    
    /// <summary>
    /// Player selects enemy vehicle - system auto-routes to appropriate component based on effect type.
    /// Used for: Most attacks, debuffs, status effects.
    /// UI: Shows enemy vehicle selector only.
    /// </summary>
    Enemy,
    
    /// <summary>
    /// Player selects enemy vehicle, then chooses specific component (precise targeting).
    /// Used for: Sniper rifles, surgical strikes, targeted disables.
    /// UI: Shows enemy vehicle selector, then component selector. Triggers two-stage attack if damage.
    /// </summary>
    EnemyComponent,
}
