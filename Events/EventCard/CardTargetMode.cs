namespace Assets.Scripts.Events.EventCard
{
    /// <summary>
    /// Defines which vehicle(s) are affected by an event card.
    /// </summary>
    public enum CardTargetMode
    {
        /// <summary>
        /// Only affects the vehicle that drew the card.
        /// </summary>
        DrawingVehicle,
        
        /// <summary>
        /// Affects all vehicles currently in the same stage.
        /// </summary>
        AllInStage
    }
}
