namespace Assets.Scripts.Managers.TurnPhases
{
    /// <summary>
    /// Handles RoundStart phase - apply round-start effects to all vehicles.
    /// Immediately advances to TurnStart.
    /// </summary>
    public class RoundStartHandler : ITurnPhaseHandler
    {
        public TurnPhase Phase => TurnPhase.RoundStart;
        
        public TurnPhase? Execute(TurnPhaseContext context)
        {
            // Future: Apply round-start effects to all vehicles
            // - Environmental effects
            // - Round-based status effect ticks
            // - etc.
            
            // Advance to first turn of the round
            return TurnPhase.TurnStart;
        }
    }
}
