namespace Assets.Scripts.Managers.TurnPhases
{
    /// <summary>
    /// Handles RoundEnd phase - apply round-end effects, advance to next round.
    /// Immediately advances to RoundStart.
    /// </summary>
    public class RoundEndHandler : ITurnPhaseHandler
    {
        public TurnPhase Phase => TurnPhase.RoundEnd;
        
        public TurnPhase? Execute(TurnPhaseContext context)
        {
            // Future: Apply round-end effects to all vehicles
            // - Round-based cooldown reductions
            // - Lasting effect cleanup
            // - etc.
            
            // Advance to next round
            return TurnPhase.RoundStart;
        }
    }
}
