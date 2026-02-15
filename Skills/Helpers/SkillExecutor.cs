using Assets.Scripts.Skills.Helpers.Resolvers;

namespace Assets.Scripts.Skills.Helpers
{
    /// <summary>
    /// Strategy pattern executor for skills.
    /// </summary>
    public static class SkillExecutor
    {
        public static bool Execute(SkillContext ctx)
        {
            if (!SkillValidator.ValidateSkillConfiguration(ctx))
                return false;

            if (!SkillValidator.ValidateTarget(ctx))
                return false;

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


