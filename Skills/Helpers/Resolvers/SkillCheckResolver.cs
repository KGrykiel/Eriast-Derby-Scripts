using Assets.Scripts.Combat.SkillChecks;
using Assets.Scripts.Combat;

namespace Assets.Scripts.Skills.Helpers.Resolvers
{
    /// <summary>
    /// Resolver for skills that use skill checks.
    /// 
    /// Flow: User rolls d20 + skill bonus vs DC
    /// - Success = effects apply
    /// - Failure = effects don't apply
    /// </summary>
    public static class SkillCheckResolver
    {
        public static bool Execute(SkillContext ctx)
        {
            Skill skill = ctx.Skill;

            // Execute: routing + computation + event emission in one call
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

