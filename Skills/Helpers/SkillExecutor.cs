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
    /// LOGGING: Events are emitted by individual resolvers via CombatEventBus.
    /// </summary>
    public static class SkillExecutor
    {
        /// <summary>
        /// Routes skill execution to the appropriate resolver.
        /// </summary>
        public static bool Execute(
            Skill skill,
            Vehicle user,
            Vehicle mainTarget,
            VehicleComponent sourceComponent = null,
            VehicleComponent targetComponent = null)
        {
            // Validate target
            if (!SkillValidator.ValidateTarget(skill, user, mainTarget))
                return false;
            
            // Validate component target if specified
            if (targetComponent != null && 
                !SkillValidator.ValidateComponentTarget(skill, user, mainTarget, targetComponent, targetComponent.name))
                return false;
            
            // Route to appropriate resolver based on roll type
            return skill.skillRollType switch
            {
                SkillRollType.AttackRoll => SkillAttackResolver.Execute(skill, user, mainTarget, sourceComponent, targetComponent),
                SkillRollType.SavingThrow => SkillSaveResolver.Execute(skill, user, mainTarget, sourceComponent, targetComponent),
                SkillRollType.SkillCheck => SkillCheckResolver.Execute(skill, user, mainTarget, sourceComponent, targetComponent),
                SkillRollType.OpposedCheck => SkillOpposedCheckResolver.Execute(skill, user, mainTarget, sourceComponent, targetComponent),
                SkillRollType.None => SkillNoRollResolver.Execute(skill, user, mainTarget, sourceComponent, targetComponent),
                _ => false
            };
        }
    }
}


