namespace Assets.Scripts.Characters
{
    /// <summary>
    /// Character skills — each governed by a CharacterAttribute.
    /// Might change in the future once we narrow down all situations that might be rolled against
    /// </summary>
    public enum CharacterSkill
    {
        // Physical Control Skills (DEX-based)
        /// <summary>Core vehicle control, useful for events related to driving</summary>
        Piloting,
        /// <summary>Mainly for evasion checks like swerving from a big rock</summary>
        DefensiveManeuvers,
        /// <summary>Used for dangerous manoeuvres or stylish driving. Will be important for the popularity system in the future</summary>
        Stunts,
        
        // Awareness Skills (WIS-based)
        /// <summary>Spotting things, same as regular DnD</summary>
        Perception,
        /// <summary>Mainly for environmental challenges so that navigator has something to do</summary>
        Survival,
        
        // Technical Skills (INT-based)
        /// <summary>Main skill for technician, related to all things mechanical on the vehicle</summary>
        Mechanics,
        /// <summary>Everything related to the magical power core. Might also be useful for other magic components</summary>
        Arcana,
        
        // Subterfuge Skills (mixed)
        /// <summary>Hiding, same like in regular DnD</summary>
        Stealth,
        /// <summary>Might be useful if we implement feinting and information warfare</summary>
        Deception,
        /// <summary>Could be good for losing aggro from AIs, maybe for some non-vehicle entities too</summary>
        Intimidation,
    }
    
    public static class CharacterSkillHelper
    {
        public static CharacterAttribute GetPrimaryAttribute(CharacterSkill skill)
        {
            return skill switch
            {
                CharacterSkill.Piloting => CharacterAttribute.Dexterity,
                CharacterSkill.DefensiveManeuvers => CharacterAttribute.Dexterity,
                CharacterSkill.Stunts => CharacterAttribute.Dexterity,

                CharacterSkill.Perception => CharacterAttribute.Wisdom,
                CharacterSkill.Survival => CharacterAttribute.Wisdom,
                
                CharacterSkill.Mechanics => CharacterAttribute.Intelligence,
                CharacterSkill.Arcana => CharacterAttribute.Intelligence,
                
                CharacterSkill.Stealth => CharacterAttribute.Dexterity,
                CharacterSkill.Deception => CharacterAttribute.Charisma,
                CharacterSkill.Intimidation => CharacterAttribute.Charisma,
                
                _ => CharacterAttribute.Dexterity
            };
        }
    }
}
