using UnityEngine;
using Combat;
using Combat.Saves;

namespace Skills.Helpers
{
    /// <summary>
    /// Resolver for skills that use saving throws.
    /// 
    /// Flow: Target rolls d20 + save bonus vs skill's DC
    /// - Save SUCCESS = target resisted = effects DON'T apply
    /// - Save FAILURE = target failed to resist = effects apply
    /// 
    /// Handles both standard (vehicle) and component targeting.
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
            // Resolve target entity
            Entity targetEntity = ResolveTargetEntity(mainTarget, targetComponent);
            if (targetEntity == null)
            {
                Debug.LogWarning($"[SkillSaveResolver] {skill.name}: Could not resolve target entity!");
                return false;
            }
            
            // Perform the saving throw (SaveCalculator handles DC calculation)
            SaveResult saveRoll = SaveCalculator.PerformSavingThrow(targetEntity, skill, user.chassis);
            
            // Emit event
            EmitSaveEvent(saveRoll, user, sourceComponent, targetEntity, targetComponent, skill);
            
            // If save succeeded, target resisted - don't apply effects
            if (saveRoll.Succeeded)
            {
                return false;
            }
            
            // Target failed save - apply effects
            SkillEffectApplicator.ApplyAllEffects(skill, user, mainTarget, sourceComponent, targetComponent);
            return true;
        }
        
        // ==================== INTERNAL METHODS ====================
        
        /// <summary>
        /// Resolve which entity the save targets.
        /// Component targeting uses the component, otherwise uses chassis.
        /// </summary>
        private static Entity ResolveTargetEntity(Vehicle mainTarget, VehicleComponent targetComponent)
        {
            if (targetComponent != null)
            {
                return targetComponent;
            }
            return mainTarget?.chassis;
        }
        
        /// <summary>
        /// Emit the appropriate combat event.
        /// </summary>
        private static void EmitSaveEvent(
            SaveResult saveRoll,
            Vehicle user,
            VehicleComponent sourceComponent,
            Entity targetEntity,
            VehicleComponent targetComponent,
            Skill skill)
        {
            Entity sourceEntity = sourceComponent != null ? sourceComponent : user.chassis;
            string targetComponentName = targetComponent?.name;
            
            CombatEventBus.EmitSavingThrow(
                saveRoll,
                sourceEntity,
                targetEntity,
                skill,
                succeeded: saveRoll.Succeeded,
                targetComponentName: targetComponentName);
        }
    }
}
