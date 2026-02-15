namespace Assets.Scripts.Managers.TurnPhases
{
    /// <summary>
    /// Interface for handling a specific turn phase, using the chain-of-responsibility pattern.
    /// </summary>
    public interface ITurnPhaseHandler
    {
        TurnPhase Phase { get; }

        /// <summary>Returns null to pause execution (player input or game over).</summary>
        TurnPhase? Execute(TurnPhaseContext context);
    }

    public class TurnPhaseContext
    {
        public TurnStateMachine StateMachine { get; }
        public TurnService TurnController { get; }
        public PlayerController PlayerController { get; }
        
        public Vehicle CurrentVehicle => StateMachine?.CurrentVehicle;
        public int CurrentRound => StateMachine != null ? StateMachine.CurrentRound : 0;
        public bool IsPlayerTurn => CurrentVehicle != null && CurrentVehicle.controlType == ControlType.Player;

        public bool IsGameOver { get; set; }
        public bool ShouldRefreshUI { get; set; }
        
        public TurnPhaseContext(TurnStateMachine stateMachine, TurnService turnController, 
                                 PlayerController playerController)
        {
            StateMachine = stateMachine;
            TurnController = turnController;
            PlayerController = playerController;
        }
    }
}
