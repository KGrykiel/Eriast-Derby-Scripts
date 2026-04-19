namespace Assets.Scripts.Managers.TurnPhases
{
    /// <summary>
    /// Guards against non-operational vehicles and starts the player UI.
    /// Returns null to pause the <see cref="TurnStateMachine"/> run loop for input.
    /// </summary>
    public class PlayerActionHandler : ITurnPhaseHandler
    {
        public TurnPhase Phase => TurnPhase.PlayerAction;

        public TurnPhase? Execute(TurnPhaseContext context)
        {
            var vehicle = context.CurrentVehicle;

            if (vehicle == null || !vehicle.IsOperational())
            {
                string reason = vehicle != null ? vehicle.GetNonOperationalReason() : "No vehicle";
                TurnEventBus.EmitPlayerCannotAct(vehicle, reason);
                return TurnPhase.TurnEnd;
            }

            context.PlayerController.InputCoordinator.BeginTurn(vehicle);
            return null;
        }
    }
}
