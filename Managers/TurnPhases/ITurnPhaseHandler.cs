namespace Assets.Scripts.Managers.TurnPhases
{
    /// <summary>
    /// Handler for a specific turn phase in the Chain of Responsibility.
    /// Each handler knows how to process its phase and what phase comes next.
    /// </summary>
    public interface ITurnPhaseHandler
    {
        /// <summary>
        /// The phase this handler is responsible for.
        /// </summary>
        TurnPhase Phase { get; }
        
        /// <summary>
        /// Process this phase. Returns the next phase to transition to.
        /// Returns null if execution should pause (waiting for player input or game over).
        /// </summary>
        TurnPhase? Execute(TurnPhaseContext context);
    }
    
    /// <summary>
    /// Context passed through the handler chain.
    /// Contains all data needed for phase processing.
    /// </summary>
    public class TurnPhaseContext
    {
        // Core references
        public TurnStateMachine StateMachine { get; }
        public TurnService TurnController { get; }
        public PlayerController PlayerController { get; }
        
        // Current state (derived from StateMachine)
        public Vehicle CurrentVehicle => StateMachine?.CurrentVehicle;
        public int CurrentRound => StateMachine != null ? StateMachine.CurrentRound : 0;
        public bool IsPlayerTurn => CurrentVehicle != null && CurrentVehicle.controlType == ControlType.Player;
        
        // Flags set by handlers
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
