using Assets.Scripts.Combat.Saves;

namespace Assets.Scripts.Skills.Helpers.Resolvers
{
    /// <summary>
    /// Handler for saving throws, uses different entry point for non-vehicle entities.
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
