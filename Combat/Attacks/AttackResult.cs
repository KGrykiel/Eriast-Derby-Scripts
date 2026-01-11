using System.Collections.Generic;
using System.Linq;

namespace Assets.Scripts.Combat.Attacks
{
    /// <summary>
    /// Category of attack roll for display purposes.
    /// </summary>
    public enum AttackCategory
    {
        Attack,         // d20 vs AC
        SkillCheck,     // d20 vs DC
        SavingThrow,    // d20 vs DC (future)
        Other           // Misc rolls
    }
    
    /// <summary>
    /// Complete result of a d20 attack roll.
    /// Pure data class - use AttackCalculator for logic, AttackResultFormatter for display.
    /// </summary>
    [System.Serializable]
    public class AttackResult
    {
        /// <summary>Category of this roll</summary>
        public AttackCategory category;
        
        /// <summary>The actual d20 result (1-20)</summary>
        public int baseRoll;
        
        /// <summary>Size of die rolled (always 20 for attacks)</summary>
        public int dieSize = 20;
        
        /// <summary>Number of dice (always 1 for attacks)</summary>
        public int diceCount = 1;
        
        /// <summary>All bonuses and penalties applied</summary>
        public List<AttackModifier> modifiers;
        
        /// <summary>Target AC/DC value</summary>
        public int targetValue;
        
        /// <summary>Name of target value ("AC", "DC", etc.)</summary>
        public string targetName = "AC";
        
        /// <summary>Whether the attack succeeded (null if not yet evaluated)</summary>
        public bool? success;
        
        /// <summary>
        /// Total roll after all modifiers (baseRoll + sum of modifiers).
        /// </summary>
        public int Total => baseRoll + modifiers.Sum(m => m.value);
        
        /// <summary>
        /// Sum of all modifiers (excluding base roll).
        /// </summary>
        public int TotalModifier => modifiers.Sum(m => m.value);
        
        public AttackResult()
        {
            modifiers = new List<AttackModifier>();
            dieSize = 20;
            diceCount = 1;
            targetName = "AC";
        }
        
        /// <summary>
        /// Create an attack result from a d20 roll.
        /// </summary>
        public static AttackResult FromD20(int baseRoll, AttackCategory category = AttackCategory.Attack)
        {
            return new AttackResult
            {
                baseRoll = baseRoll,
                dieSize = 20,
                diceCount = 1,
                category = category,
                modifiers = new List<AttackModifier>()
            };
        }
    }
}
