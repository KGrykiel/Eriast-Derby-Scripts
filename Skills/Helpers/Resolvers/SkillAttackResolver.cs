using Assets.Scripts.Combat.Attacks;

namespace Assets.Scripts.Skills.Helpers.Resolvers
{
    /// <summary>
    /// Handler for attacks skills
    /// </summary>
    public static class SkillAttackResolver
    {
        public static bool Execute(SkillContext ctx)
        {
            var spec = new AttackSpec
            {
                Target = ctx.TargetEntity,
                CausalSource = ctx.Skill,
                Attacker = ctx.SourceComponent,
                Character = ctx.SourceCharacter
            };

            var result = AttackPerformer.Execute(spec);

            if (result.HitTarget == null)
                return false;

            // Handles potential retargetting via special attack rules.
            var SkillCtx = ctx
                .WithTarget(result.HitTarget)
                .WithCriticalHit(result.Roll.IsCriticalHit);

            SkillEffectApplicator.ApplyAllEffects(SkillCtx);
            return true;
        }
    }
}
