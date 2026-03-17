namespace Assets.Scripts.Managers.TurnPhases
{
    /// <summary>
    /// Any logic to be executed at the start of a round.
    /// </summary>
    public class RoundStartHandler : ITurnPhaseHandler
    {
        public TurnPhase Phase => TurnPhase.RoundStart;

        public TurnPhase? Execute(TurnPhaseContext context)
        {
            context.StateMachine.IncrementRound();
            TurnEventBus.EmitRoundStarted(context.CurrentRound);

            return TurnPhase.TurnStart;
        }
    }
}
