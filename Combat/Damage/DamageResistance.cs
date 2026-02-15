namespace Assets.Scripts.Combat.Damage
{
    /// <summary>
    /// Struct to represent an entity's resistance or vulnerability to a specific damage type.
    /// </summary>
    [System.Serializable]
    public struct DamageResistance
    {
        public DamageType type;
        public ResistanceLevel level;
    }
}
