namespace Assets.Scripts.Skills.Helpers.Resolvers
{
    /// <summary>
    /// No roll -> apply effects directly.
    /// </summary>
    public static class SkillNoRollResolver
    {
        public static bool Execute(SkillContext ctx)
        {
            SkillEffectApplicator.ApplyAllEffects(ctx);
            return true;
        }
    }
}
