namespace Assets.Scripts.Skills.Helpers.Resolvers
{
    /// <summary>
    /// Resolver for skills that require no roll (auto-success).
    /// 
    /// Flow: No roll needed, effects always apply
    /// 
    /// Handles:
    /// - Passive abilities
    /// - Auto-success buffs/heals
    /// - Guaranteed effects
    /// </summary>
    public static class SkillNoRollResolver
    {
        /// <summary>
        /// Execute a no-roll skill.
        /// Always succeeds - applies all effects immediately.
        /// </summary>
        public static bool Execute(
            Skill skill,
            Vehicle user,
            Vehicle mainTarget,
            VehicleComponent sourceComponent,
            VehicleComponent targetComponent)
        {
            // No roll needed - apply effects directly (no critical hits possible)
            SkillEffectApplicator.ApplyAllEffects(skill, user, mainTarget, sourceComponent, targetComponent);
            return true;
        }
    }
}
