using System.Collections.Generic;
using Assets.Scripts.Entities.Vehicles;

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

    /// <summary>
    /// Shared state passed to every ITurnPhaseHandler. Holds references to all
    /// top-level systems and the per-ControlType turn controller registry.
    /// </summary>
    public class TurnPhaseContext
    {
        public TurnStateMachine StateMachine { get; }
        public TurnService TurnController { get; }
        public PlayerController PlayerController { get; }
        public VehicleActionManager ActionManager { get; }

        private readonly Dictionary<ControlType, IVehicleTurnController> turnControllers = new();

        public Vehicle CurrentVehicle => StateMachine?.CurrentVehicle;
        public int CurrentRound => StateMachine != null ? StateMachine.CurrentRound : 0;

        public IVehicleTurnController CurrentController =>
            CurrentVehicle != null && turnControllers.TryGetValue(CurrentVehicle.controlType, out var controller)
                ? controller
                : null;

        public bool IsGameOver { get; set; }
        public bool IsRaceOver { get; set; }
        public bool ShouldRefreshUI { get; set; }

        public TurnPhaseContext(TurnStateMachine stateMachine, TurnService turnController,
                                 PlayerController playerController, VehicleActionManager actionManager)
        {
            StateMachine = stateMachine;
            TurnController = turnController;
            PlayerController = playerController;
            ActionManager = actionManager;
        }

        public void RegisterController(ControlType controlType, IVehicleTurnController controller)
        {
            turnControllers[controlType] = controller;
        }
    }
}
