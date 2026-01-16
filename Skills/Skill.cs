using UnityEngine;
using System.Collections.Generic;
using Skills.Helpers;
using Combat.Saves;

public enum TargetPrecision
{
    /// <summary>
    /// Vehicle-only targeting. Always hits chassis regardless of player selection.
    /// Used for: Area attacks, cannons, non-precise weapons.
    /// UI: No component selector shown.
    /// </summary>
    VehicleOnly,

    /// <summary>
    /// Automatic routing based on effect attributes. Player targets vehicle, system routes to appropriate component.
    /// Used for: Debuffs (Slow → Drive), buffs (Shield → Chassis), most abilities.
    /// UI: No component selector shown.
    /// </summary>
    Auto,

    /// <summary>
    /// Precise targeting. Player must select specific component.
    /// Used for: Sniper rifles, targeted abilities, surgical strikes.
    /// UI: Component selector shown.
    /// </summary>
    Precise
}


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
    
    [Header("Saving Throw Configuration")]
    [Tooltip("If skillRollType = SavingThrow, what save must target make?")]
    public SaveType saveType = SaveType.Mobility;
    
    [Tooltip("Base difficulty class for saving throw (before user bonuses)")]
    public int saveDCBase = 15;

    [Header("Effects")]
    [SerializeField]
    public List<EffectInvocation> effectInvocations = new List<EffectInvocation>();
    
    [Header("Targeting")]
    [Tooltip("How precisely can this skill target components?")]
    public TargetPrecision targetPrecision = TargetPrecision.Auto;
    
    [Tooltip("Penalty when targeting protected/internal components (applied to chassis fallback)")]
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