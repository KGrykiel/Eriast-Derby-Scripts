namespace Assets.Scripts.Combat
{
    /// <summary>
    /// A single labeled contribution to a d20 roll.
    /// Purpose-built for roll breakdowns and tooltips.
    /// 
    /// This is NOT an entity modifier (AttributeModifier handles those).
    /// RollBonus is ephemeral — it exists only in a roll result for display.
    /// 
    /// Examples:
    ///   ("Chassis Mobility", +2)
    ///   ("DEX Modifier", +3)
    ///   ("Proficiency", +4)
    ///   ("Speed Boost", +1)
    ///   ("Weapon Accuracy", +3)
    /// </summary>
    public struct RollBonus
    {
        /// <summary>Display label for tooltips (e.g., "DEX Modifier", "Chassis Mobility")</summary>
        public string Label;
        
        /// <summary>Numeric value of this contribution</summary>
        public int Value;
        
        public RollBonus(string label, int value)
        {
            Label = label;
            Value = value;
        }
    }
}
