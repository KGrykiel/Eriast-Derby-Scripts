using UnityEngine;
using System.Collections.Generic;
using Assets.Scripts.Combat.SkillChecks;
using Assets.Scripts.Combat.Saves;

/// <summary>
/// Skill categories for editor organization and preset initialization.
/// Category is descriptive metadata only - does NOT enforce which effects can be used.
/// </summary>
public enum SkillCategory
{
    Attack,      // Damage-focused skills
    Restoration, // Healing/repair skills
    Buff,        // Stat enhancement skills
    Debuff,      // Status effect/penalty skills
    Utility,     // Movement, lanes, non-combat
    Special,     // Complex/custom behaviors
    Custom       // Fully manual configuration
}

/// <summary>
/// Skill data container (ScriptableObject).
/// Execution logic is handled by SkillExecutor.
/// Preset initialization is handled by SkillCreator (editor-only).
/// </summary>
public class Skill : ScriptableObject
{
    [Header("Classification")]
    [Tooltip("Category for editor organization and default presets. Potentially also for future sorting. Does not restrict effects.")]
    public SkillCategory category = SkillCategory.Attack;
    
    [Header("Basic Properties")]
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
    
    [Header("Skill Check Configuration")]
    [Tooltip("If skillRollType = SkillCheck, what check must user make?")]
    public SkillCheckType checkType = SkillCheckType.None;
    
    [Tooltip("Difficulty class for skill check")]
    public int checkDC = 15;

    [Header("Effects")]
    public List<EffectInvocation> effectInvocations = new();
    
    [Header("Targeting")]
    [Tooltip("What targeting UI flow does this skill require?")]
    public TargetingMode targetingMode = TargetingMode.Enemy;
    
    [Tooltip("Penalty when targeting protected/internal components (only for component-targeting modes)")]
    [Range(0, 10)]
    public int componentTargetingPenalty = 2;
}