namespace Assets.Scripts.Managers.TurnPhases
{
    /// <summary>
    /// Handles PlayerAction phase - PAUSES execution, waiting for player input.
    /// Returns null to signal the state machine should stop and wait.
    /// GameManager resumes by calling stateMachine.Resume() when player ends turn.
    /// </summary>
    public class PlayerActionHandler : ITurnPhaseHandler
    {
        public TurnPhase Phase => TurnPhase.PlayerAction;
        
        public TurnPhase? Execute(TurnPhaseContext context)
        {
            // PAUSE execution - return null signals "wait for external input"
            // The UI is now shown and waiting for player to take actions
            // When player clicks "End Turn", GameManager calls Resume(TurnPhase.TurnEnd)
            context.ShouldRefreshUI = true;
            return null;
        }
    }
}
