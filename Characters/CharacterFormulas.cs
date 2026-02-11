namespace Assets.Scripts.Characters
{
    /// <summary>
    /// D&D 5e formulas for character mechanics.
    /// Pure static functions - no state, no object dependencies.
    /// 
    /// Character class is a data bag - these formulas interpret that data.
    /// Follows the same pattern as WOTR's RuleCalculate* and BG3's Stats::Calculate*.
    /// </summary>
    public static class CharacterFormulas
    {
        /// <summary>
        /// Calculate attribute modifier from raw score.
        /// Standard D&D formula: (score - 10) / 2, rounded down.
        /// </summary>
        public static int CalculateAttributeModifier(int attributeScore)
        {
            return (attributeScore - 10) / 2;
        }

        /// <summary>
        /// Calculate proficiency bonus from character level.
        /// D&D 5e progression: +2 (lvl 1-4), +3 (5-8), +4 (9-12), +5 (13-16), +6 (17-20)
        /// Formula: (level - 1) / 4 + 2
        /// </summary>
        public static int CalculateProficiencyBonus(int level)
        {
            return (level - 1) / 4 + 2;
        }

        /// <summary>
        /// Calculate half-level bonus for saving throws.
        /// Used in character saves: attribute modifier + half level.
        /// </summary>
        public static int CalculateHalfLevelBonus(int level)
        {
            return level / 2;
        }

        // ==================== COMPOSITE MODIFIERS ====================
        // These combine the atomic formulas above for common use cases.
        // Single source of truth for CheckRouter and Calculators.

        /// <summary>
        /// Calculate total skill check modifier for a character.
        /// Formula: attribute modifier + proficiency (if proficient).
        /// </summary>
        public static int CalculateSkillCheckModifier(Character character, CharacterSkill skill)
        {
            CharacterAttribute attribute = CharacterSkillHelper.GetPrimaryAttribute(skill);
            int attributeScore = character.GetAttributeScore(attribute);
            int attrMod = CalculateAttributeModifier(attributeScore);
            int proficiency = character.IsProficient(skill) ? CalculateProficiencyBonus(character.level) : 0;
            return attrMod + proficiency;
        }

        /// <summary>
        /// Calculate total saving throw modifier for a character.
        /// Formula: attribute modifier + half level.
        /// </summary>
        public static int CalculateSaveModifier(Character character, CharacterAttribute attribute)
        {
            int attributeScore = character.GetAttributeScore(attribute);
            int attrMod = CalculateAttributeModifier(attributeScore);
            int halfLevel = CalculateHalfLevelBonus(character.level);
            return attrMod + halfLevel;
        }

        /// <summary>
        /// Get the character's base attack bonus.
        /// Currently a direct value on the character, but routed through here for consistency.
        /// </summary>
        public static int CalculateAttackBonus(Character character)
        {
            return character.baseAttackBonus;
        }
    }
}
