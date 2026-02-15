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
            return TurnPhase.RoundStart;
        }
    }
}
