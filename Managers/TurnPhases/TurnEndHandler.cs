using Assets.Scripts.Conditions;
using Assets.Scripts.Entities.Vehicles;

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
                if (!vehicle.HasMovedThisTurn)
                {
                    TurnEventBus.EmitAutoMovement(vehicle);
                    context.TurnController.ExecuteMovement(vehicle);
                }

                // Safety net: catches any progress changes that didn't go through ExecuteMovement (e.g. ProgressModifierEffect).
                context.TurnController.TryHandleStageTransitions(vehicle);

                vehicle.NotifyStatusEffectTrigger(RemovalTrigger.OnTurnEnd);
            }

            context.ShouldRefreshUI = true;

            if (vehicle != null)
                TurnEventBus.EmitTurnEnded(vehicle);

            bool newRound = context.StateMachine.AdvanceToNextTurn();
            return newRound ? TurnPhase.RoundEnd : TurnPhase.TurnStart;
        }
        
        }
}
