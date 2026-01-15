using UnityEngine;
using System.Collections.Generic;
using Skills.Helpers;

/// <summary>
/// Base skill data container (ScriptableObject).
/// Execution logic is handled by SkillExecutor.
/// </summary>
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
        return SkillExecutor.Execute(this, user, mainTarget);
    }

    /// <summary>
    /// Uses the skill with an optional source component.
    /// </summary>
    public virtual bool Use(Vehicle user, Vehicle mainTarget, VehicleComponent sourceComponent)
    {
        return SkillExecutor.Execute(this, user, mainTarget, sourceComponent);
    }

    /// <summary>
    /// Uses the skill with explicit component targeting.
    /// </summary>
    public virtual bool Use(Vehicle user, Vehicle mainTarget, VehicleComponent sourceComponent, VehicleComponent targetComponent)
    {
        return SkillExecutor.Execute(this, user, mainTarget, sourceComponent, targetComponent);
    }
}