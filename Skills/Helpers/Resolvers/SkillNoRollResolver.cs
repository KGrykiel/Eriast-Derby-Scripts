namespace Assets.Scripts.Skills.Helpers.Resolvers
{
    /// <summary>
    /// Resolver for skills that require no roll (auto-success).
    /// 
    /// Flow: No roll needed, effects always apply
    /// 
    /// ARCHITECTURE: Uses SkillContext for all execution data.
    /// </summary>
    public static class SkillNoRollResolver
    {
        /// <summary>
        /// Execute a no-roll skill.
        /// Always succeeds - applies all effects immediately.
        /// </summary>
        public static bool Execute(SkillContext ctx)
        {
            // No roll needed - apply effects directly
            SkillEffectApplicator.ApplyAllEffects(ctx);
            return true;
        }
    }
}
