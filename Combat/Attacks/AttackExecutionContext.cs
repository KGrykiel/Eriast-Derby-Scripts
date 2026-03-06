using Assets.Scripts.Combat.RollSpecs;
using UnityEngine;

namespace Assets.Scripts.Combat.Attacks
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
        public Object CausalSource;

        /// <summary>Entity making the attack (weapon component, turret). Provides weapon bonuses. Null for personal abilities.</summary>
        public Entity Attacker;

        /// <summary>Character making the attack. Provides attack bonus. Null for non-crew attacks.</summary>
        public Character Character;
    }
}
