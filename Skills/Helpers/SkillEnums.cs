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

    /// <summary>
    /// Targets a lane. Architecture-only — no player UI yet; AI and event cards only
    /// until lane selection UI is built.
    /// </summary>
    Lane,

    /// <summary>
    /// Targets the vehicle's own current lane directly — no selection UI shown.
    /// Use for lane-wide AoE skills that affect everyone around the caster (Oil Slick, Shockwave, etc.).
    /// UI: No selection screen shown.
    /// </summary>
    OwnLane,
}
