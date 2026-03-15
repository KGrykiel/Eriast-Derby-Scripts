using UnityEngine;
using SerializeReferenceEditor;
using Assets.Scripts.Combat.Rolls.RollSpecs;


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

    [Header("Roll")]
    [SerializeReference, SR]
    [Tooltip("The full resolution of this skill: roll type, DC, success and failure effects, optional chain.")]
    public RollNode rollNode;

    [Header("Targeting")]
    [Tooltip("What targeting UI flow does this skill require?")]
    public TargetingMode targetingMode = TargetingMode.Enemy;
}