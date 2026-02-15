using Assets.Scripts.Combat.SkillChecks;

namespace Assets.Scripts.Skills.Helpers.Resolvers
{
    /// <summary>
    /// Handler for skill checks
    /// </summary>
    public static class SkillCheckResolver
    {
        public static bool Execute(SkillContext ctx)
        {
            Skill skill = ctx.Skill;

            var checkResult = SkillCheckPerformer.Execute(
                ctx.SourceVehicle,
                skill.checkSpec,
                skill.checkDC,
                causalSource: skill,
                ctx.SourceCharacter);

            if (!checkResult.Roll.Success)
                return false;

            SkillEffectApplicator.ApplyAllEffects(ctx);
            return true;
        }
    }
}

