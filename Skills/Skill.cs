using UnityEngine;
using System.Collections.Generic;
using RacingGame.Events;
using EventType = RacingGame.Events.EventType;
using System.Linq;
using Assets.Scripts.Skills.Helpers;

public abstract class Skill : ScriptableObject
{
    public string description;
    public int energyCost = 1;

    [Header("Roll Configuration")]
    [Tooltip("Does this skill require an attack roll to hit?")]
    public bool requiresAttackRoll = true;
    
    [Tooltip("Type of roll (AC for attacks, etc.)")]
    public RollType rollType = RollType.ArmorClass;

    [Header("Effects")]
    [SerializeField]
    public List<EffectInvocation> effectInvocations = new List<EffectInvocation>();
    
    [Header("Component Targeting")]
    [Tooltip("Can this skill target specific vehicle components?")]
    public bool allowsComponentTargeting = false;
    
    [Tooltip("Penalty when targeting components (applied only to chassis fallback roll)")]
    [Range(0, 10)]
    public int componentTargetingPenalty = 2;
    
    /// <summary>
    /// Runtime-only: The name of the component being targeted (set by PlayerController).
    /// DO NOT expose to Inspector - this is set programmatically when player selects a target.
    /// </summary>
    [HideInInspector]
    public string targetComponentName = "";

    /// <summary>
    /// Uses the skill without a weapon. For spells and non-weapon abilities.
    /// </summary>
    public virtual bool Use(Vehicle user, Vehicle mainTarget)
    {
        return Use(user, mainTarget, null);
    }

    /// <summary>
    /// Uses the skill with an optional source component. Applies all effect invocations and logs the results.
    /// Supports component targeting for vehicles.
    /// </summary>
    public virtual bool Use(Vehicle user, Vehicle mainTarget, VehicleComponent sourceComponent)
    {
        // Validate target
        if (!SkillValidator.ValidateTarget(this, user, mainTarget))
            return false;

        // Check for component targeting
        if (allowsComponentTargeting && !string.IsNullOrEmpty(targetComponentName))
        {
            return UseComponentTargeted(user, mainTarget, sourceComponent);
        }

        // Standard skill resolution
        if (user.chassis == null || mainTarget.chassis == null)
        {
            Debug.LogWarning($"[Skill] {name}: User or target has no chassis!");
            return false;
        }
        
        // Perform skill roll (attack or saving throw) if required
        if (requiresAttackRoll)
        {
            RollBreakdown skillRoll = SkillAttackResolver.PerformSkillRoll(
                this, user, mainTarget.chassis, sourceComponent);
            
            if (skillRoll?.success == false)
            {
                SkillCombatLogger.LogSkillMiss(name, user, mainTarget, sourceComponent, skillRoll);
                return false;
            }
            
            // Roll succeeded - log hit
            SkillCombatLogger.LogSkillHit(name, user, mainTarget, sourceComponent, skillRoll);
        }
        
        // Apply all effects and track results
        var damageByTarget = SkillEffectApplicator.ApplyAllEffects(
            this, user, mainTarget, sourceComponent);
        
        // Log damage results (if any)
        if (damageByTarget.Count > 0)
        {
            SkillCombatLogger.LogDamageResults(name, user, damageByTarget);
        }
        
        return damageByTarget.Count > 0 || effectInvocations.Any(e => !(e.effect is DamageEffect));
    }
    
    /// <summary>
    /// Component-targeted skill resolution.
    /// For attack skills: Two-stage roll (component AC → chassis AC fallback).
    /// For non-attack skills (buffs): Applies effects directly to targeted component.
    /// </summary>
    private bool UseComponentTargeted(Vehicle user, Vehicle mainTarget, VehicleComponent sourceComponent)
    {
        VehicleComponent targetComponent = mainTarget.AllComponents
            .FirstOrDefault(c => c.name == targetComponentName);

        // Validate component accessibility
        if (!SkillValidator.ValidateComponentTarget(this, user, mainTarget, targetComponent, targetComponentName))
            return false;
        
        // For non-attack skills (buffs, etc.), apply effects directly without rolling
        if (!requiresAttackRoll)
        {
            var result = SkillEffectApplicator.ApplyAllEffects(
                this, user, mainTarget, sourceComponent, targetComponent);
            return result.Count > 0 || effectInvocations.Any(e => !(e.effect is DamageEffect));
        }
        
        // For attack skills, pre-calculate damage and attempt two-stage roll
        var damageByTarget = SkillEffectApplicator.PreCalculateDamage(
            this, user, mainTarget, sourceComponent, targetComponent);
        
        if (damageByTarget.Count == 0)
        {
            Debug.LogWarning($"[Skill] {name}: Component targeting attack skill has no damage effects!");
            return false;
        }

        // Two-stage attack: Component AC → Chassis AC (with penalty)
        return SkillAttackResolver.AttemptTwoStageComponentAttack(
            this, user, mainTarget, targetComponent, targetComponentName, sourceComponent, damageByTarget);
    }
}