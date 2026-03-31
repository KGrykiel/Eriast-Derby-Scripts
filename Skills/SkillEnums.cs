namespace Assets.Scripts.Skills
{
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

    /// <summary>
    /// Descriptive only, maybe used later for sorting or UI organisation, but does not restrict effects in any way. 
    /// Effects can be mixed and matched across categories as needed.
    /// </summary>
    public enum SkillCategory
    {
        Attack,
        Restoration,
        Buff,
        Debuff,
        Utility,
        Special,
        Custom
    }

    /// <summary>
    /// Which action resource a skill consumes when used.
    /// </summary>
    public enum ActionType
    {
        /// <summary>Standard main action — one per seat per turn.</summary>
        Action,
        /// <summary>Lighter action — one per seat per turn, independent of Action.</summary>
        BonusAction,
        /// <summary>Consumes both Action and BonusAction — for powerful abilities that dominate the whole turn.</summary>
        FullAction,
        /// <summary>No action resource consumed — always usable if other conditions are met.</summary>
        Free
    }
}