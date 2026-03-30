using System.Collections.Generic;
using Assets.Scripts.Characters;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Modifiers;

namespace Assets.Scripts.Core
{
    /// <summary>
    /// Single source of truth for character stat calculations.
    /// Owns all character formulas and applies condition modifiers from the seat's modifier list.
    /// All methods are pure — they return data, never mutate inputs.
    /// </summary>
    public static class CharacterStatCalculator
    {
        // ==================== FORMULAS ====================

        /// <summary>D&D standard: (score - 10) / 2, rounded down.</summary>
        public static int CalculateAttributeModifier(int attributeScore)
        {
            return (attributeScore - 10) / 2;
        }

        /// <summary>D&D 5e: +2 (lvl 1-4), +3 (5-8), +4 (9-12), +5 (13-16), +6 (17-20).</summary>
        public static int CalculateProficiencyBonus(int level)
        {
            return (level - 1) / 4 + 2;
        }

        public static int CalculateHalfLevelBonus(int level)
        {
            return level / 2;
        }

        /// <summary>TODO: implement a proper attackBonus formula</summary>
        public static int CalculateAttackBonus(int baseAttackBonus)
        {
            return baseAttackBonus;
        }

        // ==================== ATTRIBUTE SCORE ====================

        /// <summary>
        /// Full attribute score breakdown: base score from character sheet + condition modifiers.
        /// Returns (score, baseScore, scoreModifiers).
        /// </summary>
        public static (int score, int baseScore, List<CharacterModifier> scoreModifiers)
            GatherAttributeScoreWithBreakdown(VehicleSeat seat, CharacterAttribute attribute)
        {
            int baseScore = seat.GetAttributeScore(attribute);
            var scoreModifiers = GatherAttributeScoreModifiers(seat, attribute);
            int score = ModifierCalculator.CalculateTotal(baseScore, scoreModifiers);
            return (score, baseScore, scoreModifiers);
        }

        /// <summary>Effective attribute score after applying score-level condition modifiers.</summary>
        public static int GetAttributeScore(VehicleSeat seat, CharacterAttribute attribute)
        {
            var (score, _, _) = GatherAttributeScoreWithBreakdown(seat, attribute);
            return score;
        }

        /// <summary>Effective attribute bonus: CalculateAttributeModifier applied to the condition-adjusted score.</summary>
        public static int GetAttributeBonus(VehicleSeat seat, CharacterAttribute attribute)
        {
            return CalculateAttributeModifier(GetAttributeScore(seat, attribute));
        }

        // ==================== SKILL CHECKS ====================

        /// <summary>
        /// Full skill bonus breakdown: each component is separate for display and roll gathering.
        /// Returns (total, attrBonus, profBonus, directMods).
        /// </summary>
        public static (int total, int attrBonus, int profBonus, List<CharacterModifier> directMods)
            GatherSkillBonusWithBreakdown(VehicleSeat seat, CharacterSkill skill)
        {
            int attrBonus = GetAttributeBonus(seat, CharacterSkillHelper.GetPrimaryAttribute(skill));
            int profBonus = seat.IsProficientIn(skill) ? CalculateProficiencyBonus(seat.GetLevel()) : 0;
            var directMods = GatherDirectSkillModifiers(seat, skill);
            int total = ModifierCalculator.CalculateTotal(attrBonus + profBonus, directMods);
            return (total, attrBonus, profBonus, directMods);
        }

        /// <summary>Convenience scalar for seat comparison in routing.</summary>
        public static int GatherSkillValue(VehicleSeat seat, CharacterSkill skill)
        {
            var (total, _, _, _) = GatherSkillBonusWithBreakdown(seat, skill);
            return total;
        }

        // ==================== SAVES ====================

        /// <summary>
        /// Full save bonus breakdown: each component is separate for display and roll gathering.
        /// Returns (total, attrBonus, levelBonus).
        /// </summary>
        public static (int total, int attrBonus, int levelBonus)
            GatherSaveBonusWithBreakdown(VehicleSeat seat, CharacterAttribute attribute)
        {
            int attrBonus = GetAttributeBonus(seat, attribute);
            int levelBonus = CalculateHalfLevelBonus(seat.GetLevel());
            int total = attrBonus + levelBonus;
            return (total, attrBonus, levelBonus);
        }

        /// <summary>Convenience scalar for seat comparison in routing.</summary>
        public static int GatherSaveValue(VehicleSeat seat, CharacterAttribute attribute)
        {
            var (total, _, _) = GatherSaveBonusWithBreakdown(seat, attribute);
            return total;
        }

        // ==================== PRIVATE ====================

        private static List<CharacterModifier> GatherDirectSkillModifiers(VehicleSeat seat, CharacterSkill skill)
        {
            var modifiers = new List<CharacterModifier>();

            foreach (var mod in seat.GetCharacterModifiers())
            {
                if (mod.Value == 0) continue;

                if (mod is CharacterSkillModifier skillMod && skillMod.Skill == skill)
                    modifiers.Add(mod);
            }

            return modifiers;
        }

        private static List<CharacterModifier> GatherAttributeScoreModifiers(VehicleSeat seat, CharacterAttribute attribute)
        {
            var modifiers = new List<CharacterModifier>();

            foreach (var mod in seat.GetCharacterModifiers())
            {
                if (mod.Value == 0) continue;

                if (mod is CharacterAttributeModifier attrMod && attrMod.Attribute == attribute)
                    modifiers.Add(mod);
            }

            return modifiers;
        }
    }
}
