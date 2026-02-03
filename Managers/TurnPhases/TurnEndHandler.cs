namespace Assets.Scripts.Managers.TurnPhases
{
    /// <summary>
    /// Handles TurnEnd phase - end-of-turn effects, advance to next turn.
    /// 
    /// Turn End Order:
    /// 1. Auto-trigger movement if not moved (mandatory)
    /// 2. (Future: end-of-turn effects)
    /// 
    /// Routes to TurnStart (same round) or RoundEnd (new round).
    /// </summary>
    public class TurnEndHandler : ITurnPhaseHandler
    {
        public TurnPhase Phase => TurnPhase.TurnEnd;
        
        public TurnPhase? Execute(TurnPhaseContext context)
        {
            var vehicle = context.CurrentVehicle;
            
            // === TURN END LOGIC ===
            
            if (vehicle != null)
            {
                // 1. FORCE movement if player hasn't triggered it yet
                if (!vehicle.hasMovedThisTurn)
                {
                    TurnEventBus.EmitAutoMovement(vehicle);
                    context.TurnController.ExecuteMovement(vehicle);
                }
                
                // 2. Future: Apply end-of-turn effects
                // - Cooldown reductions
                // - Delayed damage
                // - etc.
            }
            
            // === END TURN END LOGIC ===
            
            
            // Request UI refresh after turn ends
            context.ShouldRefreshUI = true;
            
            // Advance to next turn
            bool newRound = context.StateMachine.AdvanceToNextTurn();
            
            if (newRound)
            {
                // Round counter is tracked by TurnStateMachine, no need to notify RaceHistory
                return TurnPhase.RoundEnd;
            }
            
            return TurnPhase.TurnStart;
        }
    }
}
