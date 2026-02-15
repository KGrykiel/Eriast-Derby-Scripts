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

            // Retarget if component fallback triggered
            // TODO: Probably not the right place for this, should consider a more robust way to handle this
            var effectCtx = result.WasFallback
                ? ctx.WithTarget(result.HitTarget)
                : ctx;

            SkillEffectApplicator.ApplyAllEffects(effectCtx.WithCriticalHit(result.Roll.IsCriticalHit));
            return true;
        }
    }
}
