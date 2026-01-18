using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Combat;
using Combat.Attacks;
using Combat.Saves;
using Combat.SkillChecks;
using Skills.Helpers.Resolvers;

namespace Skills.Helpers
{
    /// <summary>
    /// Routes skill execution to the appropriate resolver based on roll type.
    /// 
    /// Resolvers:
    /// - SkillAttackResolver: Attack rolls (user vs target AC)
    /// - SkillSaveResolver: Saving throws (target vs skill DC)
    /// - SkillCheckResolver: Skill checks (user vs DC)
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
                SkillRollType.SkillCheck => SkillCheckResolver.Execute(skill, user, mainTarget, sourceComponent, targetComponent),
                SkillRollType.OpposedCheck => SkillOpposedCheckResolver.Execute(skill, user, mainTarget, sourceComponent, targetComponent),
                SkillRollType.None => SkillNoRollResolver.Execute(skill, user, mainTarget, sourceComponent, targetComponent),
                _ => false
            };
        }
    }
}


