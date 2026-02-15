using UnityEngine;
using System.Collections.Generic;
using Assets.Scripts.Combat.SkillChecks;
using Assets.Scripts.Combat.Saves;


/// <summary>
/// Descriptive only, maybe used later for sorting or UI organisation, but does not restrict effects in any way. 
/// Effects can be mixed and matched across categories as needed.
/// </summary>
public enum SkillCategory
{
    Attack,
    Restoration,
    Buff,
    Debuff,
    Utility,
    Special,
    Custom
}

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
    [Tooltip("If skillRollType = SavingThrow, what save must the target make?")]
    public SaveSpec saveSpec;
    
    [Tooltip("Base difficulty class for saving throw (before user bonuses)")]
    public int saveDCBase = 15;
    
    [Header("Skill Check Configuration")]
    [Tooltip("If skillRollType = SkillCheck, what check must the user make?")]
    public SkillCheckSpec checkSpec;
    
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