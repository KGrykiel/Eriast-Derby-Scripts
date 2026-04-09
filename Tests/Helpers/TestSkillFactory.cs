using UnityEngine;
using Assets.Scripts.Characters;
using Assets.Scripts.Combat.Damage;
using Assets.Scripts.Combat.Rolls.RollSpecs;
using Assets.Scripts.Combat.Rolls.RollSpecs.SpecTypes;
using Assets.Scripts.Combat.Damage.FormulaProviders.SpecificProviders;
using Assets.Scripts.Effects.EffectTypes;
using Assets.Scripts.Effects.Targeting;
using Assets.Scripts.Combat.Rolls.Targeting;
using Assets.Scripts.Entities.Vehicles.VehicleComponents;
using Assets.Scripts.Skills;
using Assets.Scripts.Skills.Costs;
using Assets.Scripts.Effects.Invocations;
using Assets.Scripts.Effects.EffectTypes.EntityEffects;

namespace Assets.Scripts.Tests.Helpers
{
    /// <summary>
    /// Factory for creating Skill and Spec objects in tests.
    /// </summary>
    public static class TestSkillFactory
    {
        // ==================== SPECS (for Performer tests) ====================

        public static SkillCheckSpec CharacterSkillCheck(
            CharacterSkill skill,
            RoleType requiredRole = RoleType.None,
            int dc = 15)
        {
            var spec = SkillCheckSpec.ForCharacter(skill, requiredRole);
            spec.dc = dc;
            return spec;
        }

        public static SaveSpec CharacterSave(
            CharacterAttribute attribute,
            RoleType requiredRole = RoleType.None,
            int dc = 15)
        {
            var spec = SaveSpec.ForCharacter(attribute, requiredRole);
            spec.dc = dc;
            return spec;
        }

        // ==================== SKILLS (for full execution tests) ====================

        /// <summary>
        /// Create a no-roll skill (auto-applies effects).
        /// </summary>
        public static Skill CreateNoRollSkill(
            string name,
            System.Collections.Generic.List<IEffectInvocation> effects,
            System.Collections.Generic.List<Object> cleanup = null,
            params ISkillCost[] costs)
        {
            var skill = ScriptableObject.CreateInstance<Skill>();
            skill.name = name;
            foreach (var cost in costs) skill.costs.Add(cost);
            skill.rollNode = new RollNode { targetResolver = new CurrentTargetResolver(), successEffects = effects };

            cleanup?.Add(skill);
            return skill;
        }

        /// <summary>
        /// Create an attack roll skill (rolls to hit, applies damage on hit).
        /// </summary>
        public static Skill CreateAttackSkill(
            string name,
            int damageDice,
            int dieSize,
            int bonus,
            DamageType damageType = DamageType.Physical,
            System.Collections.Generic.List<Object> cleanup = null,
            params ISkillCost[] costs)
        {
            var skill = ScriptableObject.CreateInstance<Skill>();
            skill.name = name;
            foreach (var cost in costs) skill.costs.Add(cost);
            skill.rollNode = new RollNode
            {
                targetResolver = new CurrentTargetResolver(),
                rollSpec = new AttackSpec(),
                successEffects = new System.Collections.Generic.List<IEffectInvocation>
                {
                    new EntityEffectInvocation
                    {
                        effect = new DamageEffect
                        {
                            formulaProvider = new StaticFormulaProvider
                            {
                                formula = new DamageFormula
                                {
                                    baseDice = damageDice,
                                    dieSize = dieSize,
                                    bonus = bonus,
                                    damageType = damageType
                                }
                            }
                        }
                    }
                }
            };

            cleanup?.Add(skill);
            return skill;
        }

        /// <summary>
        /// Create a saving throw skill (target rolls to resist, effects apply on failure).
        /// </summary>
        public static Skill CreateSaveSkill(
            string name,
            CharacterAttribute saveAttribute,
            int dc,
            System.Collections.Generic.List<IEffectInvocation> effects,
            System.Collections.Generic.List<Object> cleanup = null,
            params ISkillCost[] costs)
        {
            var skill = ScriptableObject.CreateInstance<Skill>();
            skill.name = name;
            foreach (var cost in costs) skill.costs.Add(cost);
            // Effects go in failureEffects: the target makes the save, and effects apply when they fail it.
            var saveSpec = SaveSpec.ForCharacter(saveAttribute);
            saveSpec.dc = dc;
            skill.rollNode = new RollNode
            {
                targetResolver = new CurrentTargetResolver(),
                rollSpec = saveSpec,
                failureEffects = effects
            };

            cleanup?.Add(skill);
            return skill;
        }
    }
}
