using UnityEngine;
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
    /// 
    /// IMPORTANT: The entity making the save is determined by SaveType, NOT by targeting.
    /// Vehicle.ResolveSavingEntity() centralizes this logic.
    /// 
    /// ARCHITECTURE: Uses SkillContext for all execution data.
    /// </summary>
    public static class SkillSaveResolver
    {
        /// <summary>
        /// Execute a saving throw skill.
        /// Returns true if effects were applied (target failed save).
        /// </summary>
        public static bool Execute(SkillContext ctx)
        {
            Skill skill = ctx.Skill;
            VehicleComponent sourceComponent = ctx.SourceComponent;
            Vehicle targetVehicle = ctx.TargetVehicle;
            
            // For non-vehicle targets, use target entity directly for save
            Entity savingEntity;
            if (targetVehicle != null)
            {
                savingEntity = targetVehicle.ResolveSavingEntity(skill.saveType);
            }
            else
            {
                savingEntity = ctx.TargetEntity;
            }
            
            if (savingEntity == null)
            {
                Debug.LogWarning($"[SkillSaveResolver] {skill.name}: Could not resolve saving entity!");
                return false;
            }
            
            // Perform the saving throw (SaveCalculator handles DC calculation)
            // Use source vehicle's chassis for DC calculation if available
            Entity dcSource = sourceComponent != null ? sourceComponent : ctx.SourceVehicle != null ? ctx.SourceVehicle.chassis : null;
            SaveResult saveRoll = SaveCalculator.PerformSavingThrow(
                savingEntity, 
                skill, 
                dcSource);
            
            // Emit event
            EmitSaveEvent(saveRoll, sourceComponent, savingEntity, ctx.TargetComponent, skill);
            
            // If save succeeded, target resisted - don't apply effects
            if (saveRoll.Succeeded)
            {
                return false;
            }
            
            // Target failed save - apply effects
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
                targetComponentName: targetComponentName);
        }
    }
}
