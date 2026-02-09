namespace Assets.Scripts.Combat
{
    /// <summary>
    /// Determines whether a d20 check tests a vehicle's attribute
    /// or a character's skill/attribute.
    /// 
    /// Used by CheckSpec and SaveSpec to avoid duplicate enum hierarchies.
    /// The domain tells the resolver WHO rolls; the spec carries WHAT stat.
    /// </summary>
    public enum CheckDomain
    {
        /// <summary>Vehicle/component-based — uses Entity + vehicle Attribute.</summary>
        Vehicle,
        
        /// <summary>Character-based — uses PlayerCharacter + CharacterSkill or CharacterAttribute.</summary>
        Character
    }
}
