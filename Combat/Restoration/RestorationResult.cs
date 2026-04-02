namespace Assets.Scripts.Combat.Restoration
{
    /// <summary>
    /// Immutable result of a single restoration roll. One formula = one roll = one result.
    /// Includes both requested and actual change after target-specific clamping.
    /// </summary>
    public readonly struct RestorationResult
    {
        public ResourceType ResourceType { get; }
        public int DiceCount { get; }
        public int DieSize { get; }
        public int Bonus { get; }
        public int RawTotal { get; }
        public int OldValue { get; }
        public int NewValue { get; }
        public int MaxValue { get; }
        public int RequestedChange { get; }
        public int ActualChange { get; }

        public RestorationResult(
            ResourceType resourceType,
            int diceCount,
            int dieSize,
            int bonus,
            int rawTotal,
            int oldValue,
            int newValue,
            int maxValue,
            int requestedChange,
            int actualChange)
        {
            ResourceType = resourceType;
            DiceCount = diceCount;
            DieSize = dieSize;
            Bonus = bonus;
            RawTotal = rawTotal;
            OldValue = oldValue;
            NewValue = newValue;
            MaxValue = maxValue;
            RequestedChange = requestedChange;
            ActualChange = actualChange;
        }
    }
}
