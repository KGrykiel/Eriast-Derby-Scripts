using Assets.Scripts.Characters;
using UnityEngine;

namespace Assets.Scripts.Combat.Attacks
{
    /// <summary>
    /// Describes everything needed to execute an attack.
    /// Built by the caller, consumed by AttackPerformer.
    /// Only populate what's relevant to your situation.
    /// 
    /// Follows same pattern as SaveSpec/SkillCheckSpec.
    /// </summary>
    public class AttackSpec
    {
        // ==================== REQUIRED ====================

        /// <summary>Primary target of the attack (AC source, damage recipient)</summary>
        public Entity Target;

        /// <summary>What triggered this attack (Skill, EventCard, StatusEffect, etc.)</summary>
        public Object CausalSource;

        // ==================== BONUS CONTRIBUTORS (populate what applies) ====================

        /// <summary>Entity making the attack (weapon component, turret). Provides weapon bonuses + applied modifiers. Null for personal abilities.</summary>
        public Entity Attacker;

        /// <summary>Character making the attack. Provides base attack bonus. Null for non-crew attacks.</summary>
        public Character Character;
    }
}
