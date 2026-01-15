using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Combat;
using Combat.Attacks;

namespace Skills.Helpers
{
    /// <summary>
    /// Executes skills by orchestrating validation, rolls, and effect application.
    /// Separates execution logic from Skill data (ScriptableObject).
    /// 
    /// LOGGING: Uses CombatEventBus for all combat logging.
    /// - Attack rolls emit AttackRollEvent
    /// - Effects emit their own events (DamageEvent, StatusEffectEvent, etc.)
    /// - Events are aggregated per skill execution
    /// </summary>
    public static class SkillExecutor
    {
        /// <summary>
        /// Executes a skill with full targeting and component support.
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

            // Determine resolution strategy
            if (skill.allowsComponentTargeting && targetComponent != null)
            {
                return ExecuteComponentTargeted(skill, user, mainTarget, sourceComponent, targetComponent);
            }
            else
            {
                return ExecuteStandardTargeting(skill, user, mainTarget, sourceComponent);
            }
        }
        
        /// <summary>
        /// Standard targeting - targets vehicle (routes to appropriate component based on effect type).
        /// </summary>
        private static bool ExecuteStandardTargeting(
            Skill skill,
            Vehicle user,
            Vehicle mainTarget,
            VehicleComponent sourceComponent)
        {
            // Validate chassis exists
            if (user.chassis == null || mainTarget.chassis == null)
            {
                Debug.LogWarning($"[Skill] {skill.name}: User or target has no chassis!");
                return false;
            }
            
            // Perform roll if required
            if (skill.skillRollType != SkillRollType.None)
            {
                AttackResult skillRoll = PerformSkillRoll(skill, user, mainTarget.chassis, sourceComponent);
                
                if (skillRoll?.success != true)
                {
                    // Emit miss event
                    CombatEventBus.EmitAttackRoll(
                        skillRoll,
                        sourceComponent ?? user.chassis,
                        mainTarget.chassis,
                        skill,
                        isHit: false);
                    return false;
                }
                
                // Emit hit event
                CombatEventBus.EmitAttackRoll(
                    skillRoll,
                    sourceComponent ?? user.chassis,
                    mainTarget.chassis,
                    skill,
                    isHit: true);
            }
            
            // Apply all effects - SkillEffectApplicator handles action scoping
            SkillEffectApplicator.ApplyAllEffects(skill, user, mainTarget, sourceComponent);
            
            return true;
        }
        
        /// <summary>
        /// Component targeting - behavior depends on effect type and roll type.
        /// Damage-dealing attacks use two-stage roll (component AC → chassis AC with penalty).
        /// Other skills use single roll or no roll.
        /// </summary>
        private static bool ExecuteComponentTargeted(
            Skill skill,
            Vehicle user,
            Vehicle mainTarget,
            VehicleComponent sourceComponent,
            VehicleComponent targetComponent)
        {
            // Validate component accessibility
            if (!SkillValidator.ValidateComponentTarget(skill, user, mainTarget, targetComponent, targetComponent.name))
                return false;
            
            // Special case: Damage-dealing attacks use two-stage roll
            if (skill.skillRollType == SkillRollType.AttackRoll && HasDamageEffects(skill))
            {
                return ExecuteTwoStageComponentAttack(skill, user, mainTarget, sourceComponent, targetComponent);
            }
            
            // General case: Single roll (or no roll) + apply effects
            if (skill.skillRollType != SkillRollType.None)
            {
                AttackResult skillRoll = PerformSkillRoll(skill, user, targetComponent, sourceComponent);
                
                if (skillRoll?.success != true)
                {
                    // Emit miss event
                    CombatEventBus.EmitAttackRoll(
                        skillRoll,
                        sourceComponent ?? user.chassis,
                        targetComponent,
                        skill,
                        isHit: false,
                        targetComponent.name);
                    return false;
                }
                
                // Emit hit event
                CombatEventBus.EmitAttackRoll(
                    skillRoll,
                    sourceComponent ?? user.chassis,
                    targetComponent,
                    skill,
                    isHit: true,
                    targetComponent.name);
            }
            
            // Apply effects to the specific component
            SkillEffectApplicator.ApplyAllEffects(skill, user, mainTarget, sourceComponent, targetComponent);
            
            return true;
        }
        
        /// <summary>
        /// Two-stage component attack (only for damage-dealing attacks).
        /// Handled by SkillAttackResolver.
        /// </summary>
        private static bool ExecuteTwoStageComponentAttack(
            Skill skill,
            Vehicle user,
            Vehicle mainTarget,
            VehicleComponent sourceComponent,
            VehicleComponent targetComponent)
        {
            return SkillAttackResolver.AttemptTwoStageComponentAttack(
                skill, user, mainTarget, targetComponent, targetComponent.name, sourceComponent);
        }
        
        /// <summary>
        /// Perform a skill roll based on skillRollType.
        /// </summary>
        private static AttackResult PerformSkillRoll(
            Skill skill,
            Vehicle user,
            Entity targetEntity,
            VehicleComponent sourceComponent)
        {
            return skill.skillRollType switch
            {
                SkillRollType.AttackRoll => SkillAttackResolver.PerformSkillRoll(skill, user, targetEntity, sourceComponent),
                SkillRollType.SavingThrow => PerformSavingThrow(skill, user, targetEntity),
                SkillRollType.SkillCheck => PerformSkillCheck(skill, user, targetEntity),
                SkillRollType.OpposedCheck => PerformOpposedCheck(skill, user, targetEntity),
                _ => null
            };
        }
        
        /// <summary>
        /// Perform saving throw (target rolls to resist).
        /// TODO: Implement when saving throw system is designed.
        /// </summary>
        private static AttackResult PerformSavingThrow(Skill skill, Vehicle user, Entity targetEntity)
        {
            Debug.LogWarning($"[Skill] {skill.name}: Saving throws not yet implemented!");
            return null;
        }
        
        /// <summary>
        /// Perform skill check (user rolls vs DC).
        /// TODO: Implement when skill check system is designed.
        /// </summary>
        private static AttackResult PerformSkillCheck(Skill skill, Vehicle user, Entity targetEntity)
        {
            Debug.LogWarning($"[Skill] {skill.name}: Skill checks not yet implemented!");
            return null;
        }
        
        /// <summary>
        /// Perform opposed check (both user and target roll, highest wins).
        /// TODO: Implement when opposed check system is designed.
        /// </summary>
        private static AttackResult PerformOpposedCheck(Skill skill, Vehicle user, Entity targetEntity)
        {
            Debug.LogWarning($"[Skill] {skill.name}: Opposed checks not yet implemented!");
            return null;
        }
        
        /// <summary>
        /// Check if this skill has any damage effects.
        /// </summary>
        private static bool HasDamageEffects(Skill skill)
        {
            return skill.effectInvocations.Any(e => e.effect is DamageEffect);
        }
    }
}
