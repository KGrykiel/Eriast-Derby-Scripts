namespace Assets.Scripts.Characters
{
    /// <summary>
    /// The five core character attributes, following D&D conventions.
    /// Used for character skill checks (as governing attribute) and character saving throws.
    /// 
    /// Separate from the vehicle Attribute enum which tracks vehicle stats
    /// (MaxSpeed, ArmorClass, Mobility, etc.).
    /// </summary>
    public enum CharacterAttribute
    {
        Dexterity,
        Intelligence,
        Wisdom,
        Constitution,
        Charisma
    }
}
