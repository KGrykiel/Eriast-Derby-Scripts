using Assets.Scripts.Skills.Helpers.Resolvers;

namespace Assets.Scripts.Skills.Helpers
{
    /// <summary>
    /// Routes skill execution to the appropriate resolver based on roll type.
    /// 
    /// Resolvers:
    /// - SkillAttackResolver: Attack rolls (user vs target AC)
    /// - SkillSaveResolver: Saving throws (target vs skill DC)
    /// - SkillCheckResolver: Skill checks (user vs DC)
    /// - SkillOpposedCheckResolver: Opposed checks (user vs target)
    /// - SkillNoRollResolver: Auto-apply effects (no roll)
    /// 
    /// ARCHITECTURE: SkillContext bundles all execution data.
    /// - SourceEntity = the attacker (component or null for character skills)
    /// - TargetEntity = the target (any Entity)
    /// - SourceVehicle = who pays power (explicit, required)
    /// - SourceCharacter = optional character for bonuses
    /// 
    /// RESOURCE MANAGEMENT: Handled by Vehicle.ExecuteSkill() before calling this.
    /// LOGGING: Events are emitted by individual resolvers via CombatEventBus.
    /// </summary>
    public static class SkillExecutor
    {
        /// <summary>
        /// Routes skill execution to the appropriate resolver.
        /// Primary entry point using SkillContext.
        /// </summary>
        public static bool Execute(SkillContext ctx)
        {
            // Validate skill configuration
            if (!SkillValidator.ValidateSkillConfiguration(ctx))
                return false;
            
            // Validate target (accessibility, destroyed state)
            if (!SkillValidator.ValidateTarget(ctx))
                return false;
            
            // Route to appropriate resolver based on roll type
            return ctx.Skill.skillRollType switch
            {
                SkillRollType.AttackRoll => SkillAttackResolver.Execute(ctx),
                SkillRollType.SavingThrow => SkillSaveResolver.Execute(ctx),
                SkillRollType.SkillCheck => SkillCheckResolver.Execute(ctx),
                SkillRollType.OpposedCheck => SkillOpposedCheckResolver.Execute(ctx),
                SkillRollType.None => SkillNoRollResolver.Execute(ctx),
                _ => false
            };
        }
    }
}


