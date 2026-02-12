using UnityEngine;
using Assets.Scripts.Characters;
using Assets.Scripts.Combat.SkillChecks;
using Assets.Scripts.Combat.Saves;
using Assets.Scripts.Combat.Damage;

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
            ComponentType? requiredComponent = null)
        {
            return SkillCheckSpec.ForCharacter(skill, requiredComponent);
        }

        public static SaveSpec CharacterSave(
            CharacterAttribute attribute,
            ComponentType? requiredComponent = null)
        {
            return SaveSpec.ForCharacter(attribute, requiredComponent);
        }

        // ==================== SKILLS (for full execution tests) ====================

        /// <summary>
        /// Create a no-roll skill (auto-applies effects).
        /// </summary>
        public static Skill CreateNoRollSkill(
            string name,
            System.Collections.Generic.List<EffectInvocation> effects,
            int energyCost = 1,
            System.Collections.Generic.List<Object> cleanup = null)
        {
            var skill = ScriptableObject.CreateInstance<Skill>();
            skill.name = name;
            skill.energyCost = energyCost;
            skill.skillRollType = SkillRollType.None;
            skill.effectInvocations = effects;

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
            int energyCost = 2,
            System.Collections.Generic.List<Object> cleanup = null)
        {
            var skill = ScriptableObject.CreateInstance<Skill>();
            skill.name = name;
            skill.energyCost = energyCost;
            skill.skillRollType = SkillRollType.AttackRoll;
            skill.effectInvocations = new System.Collections.Generic.List<EffectInvocation>
            {
                new EffectInvocation
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
                    },
                    target = EffectTarget.SelectedTarget
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
            System.Collections.Generic.List<EffectInvocation> effects,
            int energyCost = 2,
            System.Collections.Generic.List<Object> cleanup = null)
        {
            var skill = ScriptableObject.CreateInstance<Skill>();
            skill.name = name;
            skill.energyCost = energyCost;
            skill.skillRollType = SkillRollType.SavingThrow;
            skill.saveSpec = SaveSpec.ForCharacter(saveAttribute);
            skill.saveDCBase = dc;
            skill.effectInvocations = effects;

            cleanup?.Add(skill);
            return skill;
        }
    }
}
