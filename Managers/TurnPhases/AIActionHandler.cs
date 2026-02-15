namespace Assets.Scripts.Managers.TurnPhases
{
    /// <summary>
    /// AI not implemented yet so for now just moves the vehicle forward.
    /// </summary>
    public class AIActionHandler : ITurnPhaseHandler
    {
        public TurnPhase Phase => TurnPhase.AIAction;

        public TurnPhase? Execute(TurnPhaseContext context)
        {
            var vehicle = context.CurrentVehicle;

            if (vehicle != null && vehicle.IsOperational())
                context.TurnController.ExecuteMovement(vehicle);

            return TurnPhase.TurnEnd;
        }
    }
}
