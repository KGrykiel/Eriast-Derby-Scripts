using UnityEngine;

namespace Assets.Scripts.Managers.TurnPhases
{
    /// <summary>
    /// Handles GameOver phase - stops execution permanently.
    /// Returns null to signal the state machine should stop.
    /// </summary>
    public class GameOverHandler : ITurnPhaseHandler
    {
        public TurnPhase Phase => TurnPhase.GameOver;
        
        public TurnPhase? Execute(TurnPhaseContext context)
        {
            Debug.Log("[GameOverHandler] Game Over - simulation stopped");
            context.IsGameOver = true;
            context.ShouldRefreshUI = true;
            return null;  // Stop execution permanently
        }
    }
}
