using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Combat;
using Combat.Attacks;
using Combat.Saves;

namespace Skills.Helpers
{
    /// <summary>
    /// Routes skill execution to the appropriate resolver based on roll type.
    /// 
    /// Resolvers:
    /// - SkillAttackResolver: Attack rolls (user vs target AC)
    /// - SkillSaveResolver: Saving throws (target vs skill DC)
    /// - (Future) SkillCheckResolver: Skill checks (user vs DC)
    /// - (Future) SkillOpposedResolver: Opposed checks (user vs target)
    /// 
    /// LOGGING: Events are emitted by individual resolvers via CombatEventBus.
    /// </summary>
    public static class SkillExecutor
    {
        /// <summary>
        /// Executes a skill with full targeting and component support.
        /// Routes to appropriate resolver based on roll type.
        /// </summary>
        public static bool Execute(
            Skill skill,
            Vehicle user,
            Vehicle mainTarget,
            VehicleComponent sourceComponent = null,
            VehicleComponent targetComponent = null)
        {
            // Validate target
            if (!SkillValidator.ValidateTarget(skill, user, mainTarget))
                return false;
            
            // Validate component target if specified
            if (targetComponent != null && 
                !SkillValidator.ValidateComponentTarget(skill, user, mainTarget, targetComponent, targetComponent.name))
                return false;
            
            // Route to appropriate resolver based on roll type
            return skill.skillRollType switch
            {
                SkillRollType.AttackRoll => SkillAttackResolver.Execute(skill, user, mainTarget, sourceComponent, targetComponent),
                SkillRollType.SavingThrow => SkillSaveResolver.Execute(skill, user, mainTarget, sourceComponent, targetComponent),
                SkillRollType.SkillCheck => ExecuteSkillCheck(skill, user, mainTarget, sourceComponent, targetComponent),
                SkillRollType.OpposedCheck => ExecuteOpposedCheck(skill, user, mainTarget, sourceComponent, targetComponent),
                SkillRollType.None => ExecuteNoRoll(skill, user, mainTarget, sourceComponent, targetComponent),
                _ => false
            };
        }
        
        // ==================== ROLL TYPE HANDLERS (stubs for future) ====================
        
        /// <summary>
        /// Handle no-roll skills (auto-success).
        /// </summary>
        private static bool ExecuteNoRoll(
            Skill skill,
            Vehicle user,
            Vehicle mainTarget,
            VehicleComponent sourceComponent,
            VehicleComponent targetComponent)
        {
            SkillEffectApplicator.ApplyAllEffects(skill, user, mainTarget, sourceComponent, targetComponent);
            return true;
        }
        
        /// <summary>
        /// Handle skill check rolls.
        /// TODO: Create SkillCheckResolver when skill check system is designed.
        /// </summary>
        private static bool ExecuteSkillCheck(
            Skill skill,
            Vehicle user,
            Vehicle mainTarget,
            VehicleComponent sourceComponent,
            VehicleComponent targetComponent)
        {
            Debug.LogWarning($"[SkillExecutor] {skill.name}: Skill checks not yet implemented!");
            return false;
        }
        
        /// <summary>
        /// Handle opposed check rolls.
        /// TODO: Create SkillOpposedResolver when opposed check system is designed.
        /// </summary>
        private static bool ExecuteOpposedCheck(
            Skill skill,
            Vehicle user,
            Vehicle mainTarget,
            VehicleComponent sourceComponent,
            VehicleComponent targetComponent)
        {
            Debug.LogWarning($"[SkillExecutor] {skill.name}: Opposed checks not yet implemented!");
            return false;
        }
    }
}
