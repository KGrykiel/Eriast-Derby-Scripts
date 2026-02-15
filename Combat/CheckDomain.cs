namespace Assets.Scripts.Combat
{
    /// <summary>
    /// enum to handle vehicle-based rolls and character-based rolls without the need of parallel hierarchies of attributes.
    /// </summary>
    public enum CheckDomain
    {
        /// <summary>Checks for vehicle as a whole (e.g. Mobility), rolled by the DM</summary>
        Vehicle,
        
        /// <summary>Checks for specific crew member, rolled by the players.</summary>
        Character
    }
}
