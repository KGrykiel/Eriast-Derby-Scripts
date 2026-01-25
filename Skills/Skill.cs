using UnityEngine;
using System.Collections.Generic;
using Assets.Scripts.Combat.SkillChecks;
using Assets.Scripts.Combat.Saves;

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
    
    [Header("Skill Check Configuration")]
    [Tooltip("If skillRollType = SkillCheck, what check must user make?")]
    public SkillCheckType checkType = SkillCheckType.Mobility;
    
    [Tooltip("Difficulty class for skill check")]
    public int checkDC = 15;

    [Header("Effects")]
    public List<EffectInvocation> effectInvocations = new List<EffectInvocation>();
    
    [Header("Targeting")]
    [Tooltip("How precisely can this skill target components?")]
    public TargetPrecision targetPrecision = TargetPrecision.Auto;
    
    [Tooltip("Penalty when targeting protected/internal components (applied to chassis fallback)")]
    [Range(0, 10)]
    public int componentTargetingPenalty = 2;
}