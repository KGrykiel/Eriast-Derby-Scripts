namespace Assets.Scripts.Combat.Damage
{
    /// <summary>
    /// A single resistance entry for an entity.
    /// Pairs a damage type with its resistance level.
    /// </summary>
    [System.Serializable]
    public struct DamageResistance
    {
        public DamageType type;
        public ResistanceLevel level;

        public DamageResistance(DamageType type, ResistanceLevel level)
        {
            this.type = type;
            this.level = level;
        }
    }
}
