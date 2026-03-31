using UnityEngine;
using SerializeReferenceEditor;
using Assets.Scripts.Combat.Rolls.RollSpecs;

namespace Assets.Scripts.Skills
{
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

        [Header("Action Economy")]
        [Tooltip("Which action resource this skill consumes. Free never blocks on the action pool.")]
        public ActionType actionCost = ActionType.Action;
    }
}