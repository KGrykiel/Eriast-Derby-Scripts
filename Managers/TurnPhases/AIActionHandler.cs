using Assets.Scripts.AI;

namespace Assets.Scripts.Managers.TurnPhases
{
    /// <summary>
    /// Runs an AI vehicle's turn. If the vehicle carries a <see cref="VehicleAIComponent"/>
    /// the full decision pipeline executes; otherwise falls back to a simple move-forward
    /// so un-authored AI vehicles still participate in the race.
    /// </summary>
    public class AIActionHandler : ITurnPhaseHandler
    {
        public TurnPhase Phase => TurnPhase.AIAction;

        public TurnPhase? Execute(TurnPhaseContext context)
        {
            var vehicle = context.CurrentVehicle;

            if (vehicle != null && vehicle.IsOperational())
            {
                var ai = vehicle.GetComponent<VehicleAIComponent>();
                if (ai != null)
                    ai.ExecuteTurn(context.TurnController);
            }

            return TurnPhase.TurnEnd;
        }
    }
}
