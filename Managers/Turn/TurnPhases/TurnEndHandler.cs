using Assets.Scripts.Conditions;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Managers.Race;
using Assets.Scripts.Visualisation;

namespace Assets.Scripts.Managers.Turn.TurnPhases
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

            if (vehicle != null && !vehicle.HasMovedThisTurn)
            {
                TurnEventBus.Emit(new AutoMovementEvent(vehicle));
                context.ActionManager.ExecuteMovementAction(
                    vehicle,
                    () => RaceMovement.ExecuteMovement(vehicle),
                    () =>
                    {
                        HideHighlight(vehicle);
                        ApplyTurnEndEffects(context, vehicle);
                        bool newRound = context.StateMachine.AdvanceToNextTurn();
                        context.StateMachine.Resume(context, newRound ? TurnPhase.RoundEnd : TurnPhase.TurnStart);
                    });
                return null;
            }

            HideHighlight(vehicle);
            ApplyTurnEndEffects(context, vehicle);
            bool wrapped = context.StateMachine.AdvanceToNextTurn();
            return wrapped ? TurnPhase.RoundEnd : TurnPhase.TurnStart;
        }

        private static void HideHighlight(Vehicle vehicle)
        {
            if (vehicle == null) return;
            VehicleVisual visual = vehicle.GetComponent<VehicleVisual>();
            if (visual != null)
                visual.HideActingHighlight();
        }

        private static void ApplyTurnEndEffects(TurnPhaseContext context, Vehicle vehicle)
        {
            if (vehicle != null)
                vehicle.NotifyStatusEffectTrigger(RemovalTrigger.OnTurnEnd);

            context.ShouldRefreshUI = true;

            if (vehicle != null)
                TurnEventBus.Emit(new TurnEndedEvent(vehicle));
        }
        
        }
}
