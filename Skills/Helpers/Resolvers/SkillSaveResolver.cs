using Assets.Scripts.Combat.Saves;
using Assets.Scripts.Combat;

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
            Vehicle targetVehicle = ctx.TargetVehicle;

            Entity dcSource = ctx.SourceComponent;
            int dc = SaveCalculator.CalculateSaveDC(skill, dcSource);
            
            SaveResult saveRoll;
            
            if (targetVehicle != null)
            {
                // Vehicle target: use vehicle-level overload (handles resolution)
                saveRoll = SaveCalculator.PerformSavingThrow(
                    targetVehicle, 
                    skill.saveSpec, 
                    dc,
                    ctx.TargetComponent);
                
                if (saveRoll == null)
                {
                    // Can't attempt save — auto-fail, apply effects
                    SkillEffectApplicator.ApplyAllEffects(ctx);
                    return true;
                }
            }
            else
            {
                // Non-vehicle target: direct save without routing
                saveRoll = SaveCalculator.PerformSavingThrowForEntity(
                    skill.saveSpec, dc, ctx.TargetEntity);
            }
            
            EmitSaveEvent(saveRoll, ctx.SourceComponent, saveRoll != null ? ctx.TargetEntity : null, ctx.TargetComponent, skill);
            
            if (saveRoll.Succeeded)
                return false;
            
            SkillEffectApplicator.ApplyAllEffects(ctx);
            return true;
        }
        
        // ==================== INTERNAL METHODS ====================
        
        /// <summary>
        /// Emit the appropriate combat event.
        /// </summary>
        private static void EmitSaveEvent(
            SaveResult saveRoll,
            VehicleComponent sourceComponent,
            Entity savingEntity,
            VehicleComponent targetComponent,
            Skill skill)
        {
            string targetComponentName = targetComponent != null ? targetComponent.name : null;
            
            CombatEventBus.EmitSavingThrow(
                saveRoll,
                sourceComponent,
                savingEntity,
                skill,
                succeeded: saveRoll.Succeeded,
                targetComponentName: targetComponentName,
                character: saveRoll.Character);  // Pass character from result
        }
    }
}
