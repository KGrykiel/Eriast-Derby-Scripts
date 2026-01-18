using UnityEngine;
using Combat;
using Combat.Saves;
using Skills.Helpers;

namespace Skills.Helpers.Resolvers
{
    /// <summary>
    /// Resolver for skills that use saving throws.
    /// 
    /// Flow: Target rolls d20 + save bonus vs skill's DC
    /// - Save SUCCESS = target resisted = effects DON'T apply
    /// - Save FAILURE = target failed to resist = effects apply
    /// 
    /// IMPORTANT: The entity making the save is determined by SaveType, NOT by targeting.
    /// Example: Mobility save always uses Chassis (which has baseMobility), even if a Weapon was targeted.
    /// The effects still apply to the originally targeted component if the save fails.
    /// </summary>
    public static class SkillSaveResolver
    {
        /// <summary>
        /// Execute a saving throw skill.
        /// Returns true if effects were applied (target failed save).
        /// </summary>
        public static bool Execute(
            Skill skill,
            Vehicle user,
            Vehicle mainTarget,
            VehicleComponent sourceComponent,
            VehicleComponent targetComponent)
        {
            // Resolve which entity makes the save (based on SaveType, not targeting)
            Entity savingEntity = ResolveSavingEntity(mainTarget, skill.saveType);
            if (savingEntity == null)
            {
                Debug.LogWarning($"[SkillSaveResolver] {skill.name}: Could not resolve saving entity!");
                return false;
            }
            
            // Perform the saving throw (SaveCalculator handles DC calculation)
            SaveResult saveRoll = SaveCalculator.PerformSavingThrow(savingEntity, skill, user.chassis);
            
            // Emit event (show which entity made the save)
            EmitSaveEvent(saveRoll, user, sourceComponent, savingEntity, targetComponent, skill);
            
            // If save succeeded, target resisted - don't apply effects
            if (saveRoll.Succeeded)
            {
                return false;
            }
            
            // Target failed save - apply effects to the originally targeted component
            SkillEffectApplicator.ApplyAllEffects(skill, user, mainTarget, sourceComponent, targetComponent);
            return true;
        }
        
        // ==================== INTERNAL METHODS ====================
        
        /// <summary>
        /// Resolve which entity makes the saving throw based on SaveType.
        /// 
        /// The saving entity is determined by which component has the relevant attribute:
        /// - Mobility saves → Chassis (has baseMobility)
        /// - Future: Systems saves → PowerCore
        /// - Future: Stability saves → Chassis
        /// 
        /// This is DIFFERENT from effect targeting - a Weapon can be targeted by effects,
        /// but the Chassis makes the Mobility save on behalf of the vehicle.
        /// </summary>
        private static Entity ResolveSavingEntity(Vehicle mainTarget, SaveType saveType)
        {
            if (mainTarget == null) return null;
            
            return saveType switch
            {
                SaveType.Mobility => mainTarget.chassis,  // Chassis has baseMobility
                // Future saves:
                // SaveType.Systems => mainTarget.powerCore,
                // SaveType.Stability => mainTarget.chassis,
                _ => mainTarget.chassis
            };
        }
        
        /// <summary>
        /// Emit the appropriate combat event.
        /// </summary>
        private static void EmitSaveEvent(
            SaveResult saveRoll,
            Vehicle user,
            VehicleComponent sourceComponent,
            Entity savingEntity,
            VehicleComponent targetComponent,
            Skill skill)
        {
            Entity sourceEntity = sourceComponent != null ? sourceComponent : user.chassis;
            string targetComponentName = targetComponent?.name;
            
            CombatEventBus.EmitSavingThrow(
                saveRoll,
                sourceEntity,
                savingEntity,  // The entity that made the save (e.g., Chassis for Mobility)
                skill,
                succeeded: saveRoll.Succeeded,
                targetComponentName: targetComponentName);
        }
    }
}
