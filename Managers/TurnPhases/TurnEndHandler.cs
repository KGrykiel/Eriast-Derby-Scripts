namespace Assets.Scripts.Managers.TurnPhases
{
    /// <summary>
    /// Any logic handled at the end of a vehicle's turn.
    /// </summary>
    public class TurnEndHandler : ITurnPhaseHandler
    {
        public TurnPhase Phase => TurnPhase.TurnEnd;

        public TurnPhase? Execute(TurnPhaseContext context)
        {
            var vehicle = context.CurrentVehicle;

            if (vehicle != null)
            {
                // Movement is mandatory, if the vehicle hasn't moved within the turn, it is automatically moved now.
                if (!vehicle.hasMovedThisTurn)
                {
                    TurnEventBus.EmitAutoMovement(vehicle);
                    context.TurnController.ExecuteMovement(vehicle);
                }

                HandleStageTransitions(vehicle, context);
            }

            context.ShouldRefreshUI = true;

            bool newRound = context.StateMachine.AdvanceToNextTurn();
            return newRound ? TurnPhase.RoundEnd : TurnPhase.TurnStart;
        }
        
        /// <summary>Transitions vehicle when progress >= stage length. Lane determines next stage.</summary>
        private void HandleStageTransitions(Vehicle vehicle, TurnPhaseContext context)
        {
            if (vehicle.currentStage == null) return;

            while (vehicle.progress >= vehicle.currentStage.length)
            {
                var currentLane = vehicle.currentLane;
                Stages.Stage nextStage = null;

                if (currentLane != null && currentLane.nextStage != null)
                    nextStage = currentLane.nextStage;
                else if (vehicle.currentStage.nextStages != null && vehicle.currentStage.nextStages.Count > 0)
                    nextStage = vehicle.currentStage.nextStages[0];

                if (nextStage != null)
                    context.TurnController.MoveToStage(vehicle, nextStage, isPlayerChoice: false);
                else
                    break;
            }
        }
    }
}
