namespace Assets.Scripts.Combat.Saves
{
    /// <summary>
    /// Save result = universal roll outcome + save-specific context.
    /// Pure data bag - no logic or factories.
    /// Access roll data through the Roll property.
    /// </summary>
    [System.Serializable]
    public class SaveResult
    {
        /// <summary>Universal roll outcome (mechanics)</summary>
        public D20RollOutcome Roll { get; }

        /// <summary>What was tested (vehicle attribute or character attribute)</summary>
        public SaveSpec Spec { get; }

        /// <summary>Character who made the save (null for vehicle-only saves)</summary>
        public Character Character { get; }

        /// <summary>True if required component was missing (no roll occurred)</summary>
        public bool IsAutoFail { get; }

        public SaveResult(D20RollOutcome roll, SaveSpec spec, Character character = null, bool isAutoFail = false)
        {
            Roll = roll;
            Spec = spec;
            Character = character;
            IsAutoFail = isAutoFail;
        }
    }
}

