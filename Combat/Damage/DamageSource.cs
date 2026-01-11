using System;
using System.Collections.Generic;
using System.Text;

namespace Assets.Scripts.Combat.Damage
{
    /// <summary>
    /// A single source of damage within a damage calculation.
    /// Represents one dice pool (weapon dice, skill dice, sneak attack, etc.)
    /// 
    /// NOTE: Named DamageSourceEntry to avoid conflict with DamageSource enum.
    /// </summary>
    [System.Serializable]
    public struct DamageSourceEntry
    {
        /// <summary>Display name: "Weapon", "Skill Bonus", "Sneak Attack"</summary>
        public string name;
        
        /// <summary>Number of dice</summary>
        public int diceCount;
        
        /// <summary>Size of dice (d6, d8, etc.)</summary>
        public int dieSize;
        
        /// <summary>Flat bonus added to this source</summary>
        public int bonus;
        
        /// <summary>Sum of dice rolled (without bonus)</summary>
        public int rolled;
        
        /// <summary>Source name for attribution: "Heavy Crossbow", "Power Shot"</summary>
        public string sourceName;
        
        /// <summary>
        /// Total damage from this source (rolled + bonus).
        /// </summary>
        public int Total => rolled + bonus;
        
        public DamageSourceEntry(string name, int diceCount, int dieSize, int bonus, int rolled, string sourceName = null)
        {
            this.name = name;
            this.diceCount = diceCount;
            this.dieSize = dieSize;
            this.bonus = bonus;
            this.rolled = rolled;
            this.sourceName = sourceName ?? name;
        }
        
        /// <summary>
        /// Format as dice notation: "2d6+3" or "+5" for flat bonuses
        /// </summary>
        public string ToDiceString()
        {
            if (diceCount <= 0 || dieSize <= 0)
            {
                // Flat bonus only
                return bonus >= 0 ? $"+{bonus}" : $"{bonus}";
            }
            
            string result = $"{diceCount}d{dieSize}";
            if (bonus > 0) result += $"+{bonus}";
            else if (bonus < 0) result += $"{bonus}";
            return result;
        }
        
        public override string ToString() => $"{name}: {ToDiceString()} = {Total}";
    }
}
