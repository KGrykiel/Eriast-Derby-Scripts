namespace Assets.Scripts.Combat
{
    /// <summary>
    /// Labeled contribution to a d20 roll, for breakdowns and tooltips.
    /// Ephemeral — NOT a persistent entity modifier (that's AttributeModifier).
    /// </summary>
    public struct RollBonus
    {
        public string Label;
        public int Value;
        
        public RollBonus(string label, int value)
        {
            Label = label;
            Value = value;
        }
    }
}
