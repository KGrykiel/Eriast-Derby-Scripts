using Assets.Scripts.StatusEffects;

namespace Assets.Scripts.Managers.TurnPhases
{
    /// <summary>
    /// Any logic to be executed upon the end of a round (after all vehicles have taken their turns)
    /// </summary>
    public class RoundEndHandler : ITurnPhaseHandler
    {
        public TurnPhase Phase => TurnPhase.RoundEnd;

        public TurnPhase? Execute(TurnPhaseContext context)
        {
            foreach (var vehicle in context.TurnController.AllVehicles)
                vehicle.NotifyStatusEffectTrigger(RemovalTrigger.OnRoundEnd);

            TurnEventBus.EmitRoundEnded(context.CurrentRound);

            return TurnPhase.RoundStart;
        }
    }
}
