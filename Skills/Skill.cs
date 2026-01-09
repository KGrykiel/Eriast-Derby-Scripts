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
    [Tooltip("What type of roll does this skill require?")]
    public SkillRollType skillRollType = SkillRollType.None;

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
    /// Uses the skill without a source component. For spells and non-weapon abilities.
    /// </summary>
    public virtual bool Use(Vehicle user, Vehicle mainTarget)
    {
        return Use(user, mainTarget, null);
    }

    /// <summary>
    /// Uses the skill with an optional source component. Applies all effect invocations and logs the results.
    /// Standard targeting - targets vehicle's chassis.
    /// </summary>
    public virtual bool Use(Vehicle user, Vehicle mainTarget, VehicleComponent sourceComponent)
    {
        return Use(user, mainTarget, sourceComponent, null);
    }

    /// <summary>
    /// Uses the skill with explicit component targeting.
    /// If targetComponent is provided, uses component-targeted resolution (two-stage rolls for attacks).
    /// If targetComponent is null, uses standard resolution.
    /// </summary>
    public virtual bool Use(Vehicle user, Vehicle mainTarget, VehicleComponent sourceComponent, VehicleComponent targetComponent)
    {
        // Validate target
        if (!SkillValidator.ValidateTarget(this, user, mainTarget))
            return false;

        // Determine resolution strategy
        if (allowsComponentTargeting && targetComponent != null)
        {
            return UseComponentTargeted(user, mainTarget, sourceComponent, targetComponent);
        }
        else
        {
            return UseStandardTargeting(user, mainTarget, sourceComponent);
        }
    }
    
    /// <summary>
    /// Standard targeting - targets vehicle (routes to appropriate component based on effect type).
    /// </summary>
    private bool UseStandardTargeting(Vehicle user, Vehicle mainTarget, VehicleComponent sourceComponent)
    {
        // Validate chassis exists
        if (user.chassis == null || mainTarget.chassis == null)
        {
            Debug.LogWarning($"[Skill] {name}: User or target has no chassis!");
            return false;
        }
        
        // Perform roll if required
        if (skillRollType != SkillRollType.None)
        {
            RollBreakdown skillRoll = PerformSkillRoll(user, mainTarget.chassis, sourceComponent);
            
            if (skillRoll?.success != true)
            {
                SkillCombatLogger.LogSkillMiss(name, user, mainTarget, sourceComponent, skillRoll);
                return false;
            }
            
            // Roll succeeded - log hit
            SkillCombatLogger.LogSkillHit(name, user, mainTarget, sourceComponent, skillRoll);
        }
        
        // Apply all effects (routing handles destination)
        var damageByTarget = SkillEffectApplicator.ApplyAllEffects(
            this, user, mainTarget, sourceComponent);
        
        // Log damage results (if any)
        if (damageByTarget.Count > 0)
        {
            SkillCombatLogger.LogDamageResults(name, user, damageByTarget);
        }
        
        return damageByTarget.Count > 0 || HasNonDamageEffects();
    }
    
    /// <summary>
    /// Component targeting - behavior depends on effect type and roll type.
    /// Damage-dealing attacks use two-stage roll (component AC → chassis AC with penalty).
    /// Other skills use single roll or no roll.
    /// </summary>
    private bool UseComponentTargeted(Vehicle user, Vehicle mainTarget, VehicleComponent sourceComponent, VehicleComponent targetComponent)
    {
        // Validate component accessibility
        if (!SkillValidator.ValidateComponentTarget(this, user, mainTarget, targetComponent, targetComponent.name))
            return false;
        
        // Special case: Damage-dealing attacks use two-stage roll
        if (skillRollType == SkillRollType.AttackRoll && HasDamageEffects())
        {
            return UseTwoStageComponentAttack(user, mainTarget, sourceComponent, targetComponent);
        }
        
        // General case: Single roll (or no roll) + apply effects
        if (skillRollType != SkillRollType.None)
        {
            RollBreakdown skillRoll = PerformSkillRoll(user, targetComponent, sourceComponent);
            
            if (skillRoll?.success != true)
            {
                SkillCombatLogger.LogSkillMiss(name, user, mainTarget, sourceComponent, skillRoll);
                return false;
            }
            
            SkillCombatLogger.LogSkillHit(name, user, mainTarget, sourceComponent, skillRoll);
        }
        
        // Apply effects to the specific component
        var results = SkillEffectApplicator.ApplyAllEffects(
            this, user, mainTarget, sourceComponent, targetComponent);
        
        if (results.Count > 0)
        {
            SkillCombatLogger.LogDamageResults(name, user, results);
        }
        
        return results.Count > 0 || HasNonDamageEffects();
    }
    
    /// <summary>
    /// Two-stage component attack (only for damage-dealing attacks).
    /// Stage 1: Roll vs Component AC (no penalty)
    /// Stage 2: Roll vs Chassis AC (with penalty)
    /// Damage is calculated only after successful hit.
    /// </summary>
    private bool UseTwoStageComponentAttack(Vehicle user, Vehicle mainTarget, VehicleComponent sourceComponent, VehicleComponent targetComponent)
    {
        // Attempt two-stage roll (damage calculated after hit)
        return SkillAttackResolver.AttemptTwoStageComponentAttack(
            this, user, mainTarget, targetComponent, targetComponent.name, sourceComponent);
    }
    
    /// <summary>
    /// Perform a skill roll based on skillRollType.
    /// Returns null if roll failed or RollBreakdown if successful.
    /// </summary>
    private RollBreakdown PerformSkillRoll(Vehicle user, Entity targetEntity, VehicleComponent sourceComponent)
    {
        return skillRollType switch
        {
            SkillRollType.AttackRoll => SkillAttackResolver.PerformSkillRoll(this, user, targetEntity, sourceComponent),
            SkillRollType.SavingThrow => PerformSavingThrow(user, targetEntity),
            SkillRollType.SkillCheck => PerformSkillCheck(user, targetEntity),
            SkillRollType.OpposedCheck => PerformOpposedCheck(user, targetEntity),
            _ => null
        };
    }
    
    /// <summary>
    /// Perform saving throw (target rolls to resist).
    /// TODO: Implement when saving throw system is designed.
    /// </summary>
    private RollBreakdown PerformSavingThrow(Vehicle user, Entity targetEntity)
    {
        Debug.LogWarning($"[Skill] {name}: Saving throws not yet implemented!");
        return null;
    }
    
    /// <summary>
    /// Perform skill check (user rolls vs DC).
    /// TODO: Implement when skill check system is designed.
    /// </summary>
    private RollBreakdown PerformSkillCheck(Vehicle user, Entity targetEntity)
    {
        Debug.LogWarning($"[Skill] {name}: Skill checks not yet implemented!");
        return null;
    }
    
    /// <summary>
    /// Perform opposed check (both user and target roll, highest wins).
    /// TODO: Implement when opposed check system is designed.
    /// </summary>
    private RollBreakdown PerformOpposedCheck(Vehicle user, Entity targetEntity)
    {
        Debug.LogWarning($"[Skill] {name}: Opposed checks not yet implemented!");
        return null;
    }
    
    /// <summary>
    /// Check if this skill has any damage effects.
    /// </summary>
    private bool HasDamageEffects()
    {
        return effectInvocations.Any(e => e.effect is DamageEffect);
    }
    
    /// <summary>
    /// Check if this skill has any non-damage effects.
    /// </summary>
    private bool HasNonDamageEffects()
    {
        return effectInvocations.Any(e => !(e.effect is DamageEffect));
    }
}