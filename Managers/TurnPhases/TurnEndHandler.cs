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
                
                // 2. Check for stage transitions (lane-based system)
                HandleStageTransitions(vehicle, context);
                
                // 3. Future: Apply end-of-turn effects
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
        
        /// <summary>
        /// Handle stage transitions based on lane system.
        /// Vehicle's current lane determines which stage they enter next.
        /// Automatically transitions when progress >= stage length.
        /// </summary>
        private void HandleStageTransitions(Vehicle vehicle, TurnPhaseContext context)
        {
            if (vehicle.currentStage == null) return;
            
            // Loop while vehicle has enough progress to exit current stage
            while (vehicle.progress >= vehicle.currentStage.length)
            {
                var currentLane = vehicle.currentLane;
                Stages.Stage nextStage = null;
                
                // Determine next stage from lane system
                if (currentLane != null && currentLane.nextStage != null)
                {
                    // Lane specifies which stage to enter (tactical positioning!)
                    nextStage = currentLane.nextStage;
                }
                else if (vehicle.currentStage.nextStages != null && vehicle.currentStage.nextStages.Count > 0)
                {
                    // Fallback: Use first stage from Stage.nextStages list
                    nextStage = vehicle.currentStage.nextStages[0];
                }
                
                // Perform transition if we have a valid next stage
                if (nextStage != null)
                {
                    context.TurnController.MoveToStage(vehicle, nextStage, isPlayerChoice: false);
                }
                else
                {
                    // No next stage - reached end of track or finish line
                    break;
                }
            }
        }
    }
}
