namespace Assets.Scripts.Characters
{
    /// <summary>
    /// D&D 5e formulas for character mechanics. Pure static functions, no state.
    /// </summary>
    public static class CharacterFormulas
    {
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

        // ==================== COMPOSITE MODIFIERS ====================
        // These combine the atomic formulas above for common use cases.
        // Single source of truth for CheckRouter and Calculators.

        public static int CalculateSkillCheckModifier(Character character, CharacterSkill skill)
        {
            CharacterAttribute attribute = CharacterSkillHelper.GetPrimaryAttribute(skill);
            int attributeScore = character.GetAttributeScore(attribute);
            int attrMod = CalculateAttributeModifier(attributeScore);
            int proficiency = character.IsProficient(skill) ? CalculateProficiencyBonus(character.level) : 0;
            return attrMod + proficiency;
        }

        public static int CalculateSaveModifier(Character character, CharacterAttribute attribute)
        {
            int attributeScore = character.GetAttributeScore(attribute);
            int attrMod = CalculateAttributeModifier(attributeScore);
            int halfLevel = CalculateHalfLevelBonus(character.level);
            return attrMod + halfLevel;
        }

        /// <summary>TODO: implement a proper attackBonus formula</summary>
        public static int CalculateAttackBonus(Character character)
        {
            return character.baseAttackBonus;
        }
    }
}
