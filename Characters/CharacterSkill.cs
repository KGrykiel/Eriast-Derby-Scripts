namespace Assets.Scripts.Characters
{
    /// <summary>
    /// The 10 character skills representing training and expertise.
    /// Each skill is governed by a CharacterAttribute and can be ranked 0-10+.
    /// 
    /// This is a character system concept — it represents what a character knows,
    /// independent of the combat check system. For combat resolution, use
    /// SkillCheckType which includes both character skills and vehicle attribute checks.
    /// </summary>
    public enum CharacterSkill
    {
        // Physical Control Skills (DEX-based)
        /// <summary>Core vehicle control, maintaining speed, basic maneuvers.</summary>
        Piloting,
        /// <summary>Evasive driving, swerving to avoid obstacles, defensive positioning.</summary>
        DefensiveManeuvers,
        /// <summary>Risky tricks, showmanship, pushing vehicle beyond normal limits.</summary>
        Stunts,
        
        // Awareness Skills (WIS-based)
        /// <summary>Spotting enemies, hazards, and opportunities.</summary>
        Perception,
        /// <summary>Reading terrain, predicting environmental challenges.</summary>
        Survival,
        
        // Technical Skills (INT-based)
        /// <summary>Physical repair, maintenance, hands-on engineering.</summary>
        Mechanics,
        /// <summary>Understanding magical systems, power cores, enchantments.</summary>
        Arcana,
        
        // Subterfuge Skills (mixed)
        /// <summary>Hiding from scans, jamming enemy sensors (DEX-based).</summary>
        Stealth,
        /// <summary>Bluffing intentions, feinting maneuvers (CHA-based).</summary>
        Deception,
        /// <summary>Psychological warfare, forcing enemy mistakes (CHA-based).</summary>
        Intimidation,
    }
    
    /// <summary>
    /// Helper methods for CharacterSkill.
    /// </summary>
    public static class CharacterSkillHelper
    {
        /// <summary>
        /// Get the governing CharacterAttribute for a skill.
        /// This determines which attribute modifier is added to skill checks.
        /// </summary>
        public static CharacterAttribute GetPrimaryAttribute(CharacterSkill skill)
        {
            return skill switch
            {
                // Physical Control Skills (DEX-based)
                CharacterSkill.Piloting => CharacterAttribute.Dexterity,
                CharacterSkill.DefensiveManeuvers => CharacterAttribute.Dexterity,
                CharacterSkill.Stunts => CharacterAttribute.Dexterity,
                
                // Awareness Skills (WIS-based)
                CharacterSkill.Perception => CharacterAttribute.Wisdom,
                CharacterSkill.Survival => CharacterAttribute.Wisdom,
                
                // Technical Skills (INT-based)
                CharacterSkill.Mechanics => CharacterAttribute.Intelligence,
                CharacterSkill.Arcana => CharacterAttribute.Intelligence,
                
                // Subterfuge Skills (mixed)
                CharacterSkill.Stealth => CharacterAttribute.Dexterity,
                CharacterSkill.Deception => CharacterAttribute.Charisma,
                CharacterSkill.Intimidation => CharacterAttribute.Charisma,
                
                _ => CharacterAttribute.Dexterity
            };
        }
    }
}
