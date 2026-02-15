namespace Assets.Scripts.Events.EventCard
{
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
