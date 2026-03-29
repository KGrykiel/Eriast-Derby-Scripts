using Assets.Scripts.Combat.Rolls.RollSpecs.SpecTypes;
using Assets.Scripts.Entities;

namespace Assets.Scripts.Combat.Rolls.RollTypes.Attacks
{
    /// <summary>
    /// All information needed to perform an attack, passed to AttackPerformer.
    /// Follows same pattern as SaveSpec/SkillCheckSpec.
    /// </summary>
    public class AttackExecutionContext
    {
        /// <summary>Attack specification — designer-configured fields.</summary>
        public AttackSpec Spec;

        /// <summary>Entity being attacked.</summary>
        public Entity Target;

        /// <summary>What triggered this attack (Skill, EventCard, StatusEffect, etc.). Used for logging.</summary>
        public string CausalSource;

        /// <summary>Who or what is making the attack. Provides weapon bonuses (if component) and/or attack bonus (if character).</summary>
        public RollActor Attacker;
    }
}
