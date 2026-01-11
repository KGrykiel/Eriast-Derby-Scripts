using System;
using System.Collections.Generic;
using System.Text;

namespace Assets.Scripts.Combat.Attacks
{
    /// <summary>
    /// A single modifier contributing to an attack roll.
    /// Tracks the source of each bonus/penalty for transparent logging.
    /// </summary>
    [System.Serializable]
    public struct AttackModifier
    {
        /// <summary>Display name: "Weapon Attack Bonus", "Component Targeting Penalty"</summary>
        public string name;
        
        /// <summary>Modifier value: +2 or -3</summary>
        public int value;
        
        /// <summary>Source of modifier: "Heavy Crossbow", "Power Shot"</summary>
        public string source;
        
        public AttackModifier(string name, int value, string source = null)
        {
            this.name = name;
            this.value = value;
            this.source = source ?? name;
        }
        
        public override string ToString()
        {
            string sign = value >= 0 ? "+" : "";
            return $"{name}: {sign}{value}";
        }
    }
}
