namespace Assets.Scripts.Managers.TurnPhases
{
    /// <summary>
    /// Everything happening at the start of a vehicle's turn.
    /// Routes to PlayerAction or AIAction based on vehicle control type.
    /// </summary>
    public class TurnStartHandler : ITurnPhaseHandler
    {
        public TurnPhase Phase => TurnPhase.TurnStart;

        public TurnPhase? Execute(TurnPhaseContext context)
        {
            var vehicle = context.CurrentVehicle;

            if (context.StateMachine.ShouldSkipTurn(vehicle))
                return AdvanceToNextTurnOrRoundEnd(context);

            // === TURN START LOGIC ===
            
            // 1. Regenerate power FIRST (see full resources before paying costs)
            if (vehicle.PowerCore != null && !vehicle.PowerCore.IsDestroyed())
                vehicle.PowerCore.RegenerateEnergy();

            // 2. Reset per-turn power tracking
            if (vehicle.PowerCore != null)
                vehicle.PowerCore.ResetTurnPowerTracking();

            // 3. Accelerate toward target speed
            context.TurnController.AccelerateVehicle(vehicle);
            
            // 4. Draw continuous power for all components (emits shutdown events via TurnEventBus)
            context.TurnController.DrawContinuousPowerForAllComponents(vehicle);

            // 5. Reset movement and seat turn states
            vehicle.ResetComponentsForNewTurn();

            // 6. Update status effects at turn start
            vehicle.UpdateStatusEffects();

            // 7. Process lane turn effects (hazards, environmental checks)
            if (vehicle.CurrentStage != null)
                vehicle.CurrentStage.ProcessLaneTurnEffects(vehicle);

            TurnEventBus.EmitTurnStarted(vehicle);

            if (context.IsPlayerTurn)
            {
                context.PlayerController.ProcessPlayerMovement();
                return TurnPhase.PlayerAction;
            }

            return TurnPhase.AIAction;
        }
        
        private TurnPhase? AdvanceToNextTurnOrRoundEnd(TurnPhaseContext context)
        {
            bool newRound = context.StateMachine.AdvanceToNextTurn();
            return newRound ? TurnPhase.RoundEnd : TurnPhase.TurnStart;
        }
    }
}
