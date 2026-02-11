using Assets.Scripts.Combat.Saves;

namespace Assets.Scripts.Skills.Helpers.Resolvers
{
    /// <summary>
    /// Resolver for skills that use saving throws.
    /// 
    /// Flow: Target rolls d20 + save bonus vs skill's DC
    /// - Save SUCCESS = target resisted = effects DON'T apply
    /// - Save FAILURE = target failed to resist = effects apply
    /// </summary>
    public static class SkillSaveResolver
    {
        public static bool Execute(SkillContext ctx)
        {
            Skill skill = ctx.Skill;
            int dc = SaveCalculator.CalculateSaveDC(skill);

            SaveResult saveRoll;

            if (ctx.TargetVehicle != null)
            {
                // Vehicle target: use performer (handles routing + computation + event)
                saveRoll = SavePerformer.Execute(
                    ctx.TargetVehicle,
                    skill.saveSpec,
                    dc,
                    causalSource: skill,
                    ctx.TargetComponent,
                    ctx.SourceComponent);
            }
            else
            {
                // Non-vehicle target: direct computation without routing
                saveRoll = SavePerformer.ExecuteForEntity(
                    ctx.TargetEntity,
                    skill.saveSpec,
                    dc,
                    causalSource: skill,
                    attackerEntity: ctx.SourceComponent);
            }

            if (saveRoll.Roll.Success)
                return false;

            SkillEffectApplicator.ApplyAllEffects(ctx);
            return true;
        }
    }
}
