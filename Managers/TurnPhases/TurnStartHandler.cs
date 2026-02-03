namespace Assets.Scripts.Managers.TurnPhases
{
    /// <summary>
    /// Handles TurnStart phase - the beginning of a vehicle's turn.
    /// 
    /// Turn Start Order:
    /// 1. Regenerate power
    /// 2. Reset per-turn power tracking
    /// 3. Accelerate toward target speed
    /// 4. Draw continuous power for all components
    /// 5. Reset movement flag
    /// 6. Reset seat/component states
    /// 7. Update status effects
    /// 8. Process lane turn effects
    /// 
    /// Routes to PlayerAction or AIAction based on vehicle control type.
    /// </summary>
    public class TurnStartHandler : ITurnPhaseHandler
    {
        public TurnPhase Phase => TurnPhase.TurnStart;
        
        public TurnPhase? Execute(TurnPhaseContext context)
        {
            var vehicle = context.CurrentVehicle;
            
            // Skip destroyed or invalid vehicles
            if (context.StateMachine.ShouldSkipTurn(vehicle))
            {
                return AdvanceToNextTurnOrRoundEnd(context);
            }
            
            // === TURN START LOGIC ===
            
            // 1. Regenerate power FIRST (see full resources before paying costs)
            if (vehicle.powerCore != null && !vehicle.powerCore.isDestroyed)
            {
                vehicle.powerCore.RegenerateEnergy();
            }
            
            // 2. Reset per-turn power tracking
            if (vehicle.powerCore != null)
            {
                vehicle.powerCore.ResetTurnPowerTracking();
            }
            
            // 3. Accelerate toward target speed
            context.TurnController.AccelerateVehicle(vehicle);
            
            // 4. Draw continuous power for all components (emits shutdown events via TurnEventBus)
            context.TurnController.DrawContinuousPowerForAllComponents(vehicle);
            
            // 5. Reset movement flag
            vehicle.hasMovedThisTurn = false;
            vehicle.hasLoggedMovementWarningThisTurn = false;
            
            // 6. Reset seat turn states
            vehicle.ResetComponentsForNewTurn();
            
            // 7. Update status effects at turn start
            vehicle.UpdateStatusEffects();
            
            // 8. Process lane turn effects (hazards, environmental checks)
            if (vehicle.currentStage != null)
            {
                vehicle.currentStage.ProcessLaneTurnEffects(vehicle);
            }
            
            // === END TURN START LOGIC ===
            
            // Route based on control type
            if (context.IsPlayerTurn)
            {
                context.PlayerController.ProcessPlayerMovement();
                return TurnPhase.PlayerAction;
            }
            
            return TurnPhase.AIAction;
        }
        
        /// <summary>
        /// Skip this turn and advance to next, or to RoundEnd if this was the last turn.
        /// </summary>
        private TurnPhase? AdvanceToNextTurnOrRoundEnd(TurnPhaseContext context)
        {
            bool newRound = context.StateMachine.AdvanceToNextTurn();
            // Round counter is tracked by TurnStateMachine, no need to notify RaceHistory
            return newRound ? TurnPhase.RoundEnd : TurnPhase.TurnStart;
        }
    }
}
