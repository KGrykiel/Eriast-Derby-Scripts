namespace Assets.Scripts.Managers.TurnPhases
{
    /// <summary>
    /// Pauses execution — returns null to wait for player input.
    /// </summary>
    public class PlayerActionHandler : ITurnPhaseHandler
    {
        public TurnPhase Phase => TurnPhase.PlayerAction;

        public TurnPhase? Execute(TurnPhaseContext context)
        {
            context.ShouldRefreshUI = true;
            return null;
        }
    }
}
