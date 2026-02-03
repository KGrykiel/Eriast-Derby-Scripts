using System;
using Assets.Scripts.Stages;

namespace Assets.Scripts.Managers
{
    /// <summary>
    /// Centralized event bus for ALL turn-related events.
    /// Single source of truth for turn lifecycle, operations, and player actions.
    /// 
    /// Event Categories:
    /// - Lifecycle: Phase changes, round/turn start/end, game over
    /// - Operations: Movement, power, stage transitions
    /// - Player: Player-specific actions and state changes
    /// 
    /// All turn-related systems emit here; TurnEventLogger subscribes.
    /// Matches CombatEventBus pattern for consistency.
    /// </summary>
    public static class TurnEventBus
    {
        // ==================== LIFECYCLE EVENTS ====================
        
        /// <summary>Fired when phase changes. Args: (oldPhase, newPhase)</summary>
        public static event Action<TurnPhase, TurnPhase> OnPhaseChanged;
        
        /// <summary>Fired at start of each round. Args: roundNumber</summary>
        public static event Action<int> OnRoundStarted;
        
        /// <summary>Fired at end of each round. Args: roundNumber</summary>
        public static event Action<int> OnRoundEnded;
        
        /// <summary>Fired at start of a vehicle's turn. Args: vehicle</summary>
        public static event Action<Vehicle> OnTurnStarted;
        
        /// <summary>Fired at end of a vehicle's turn. Args: vehicle</summary>
        public static event Action<Vehicle> OnTurnEnded;
        
        /// <summary>Fired when game ends</summary>
        public static event Action OnGameOver;
        
        /// <summary>Fired when initiative is rolled. Args: (vehicle, initiativeValue)</summary>
        public static event Action<Vehicle, int> OnInitiativeRolled;
        
        /// <summary>Fired when a vehicle is removed from turn order (by state machine). Args: vehicle</summary>
        public static event Action<Vehicle> OnVehicleRemoved;
        
        /// <summary>Fired when a vehicle is destroyed (by Vehicle itself). Args: vehicle</summary>
        public static event Action<Vehicle> OnVehicleDestroyed;
        
        // ==================== MOVEMENT EVENTS ====================
        
        /// <summary>Fired when a vehicle auto-moves at turn end (didn't move manually)</summary>
        public static event Action<Vehicle> OnAutoMovement;
        
        /// <summary>Fired when movement is blocked. Args: (vehicle, reason)</summary>
        public static event Action<Vehicle, string> OnMovementBlocked;
        
        /// <summary>Fired when movement executes. Args: (vehicle, distance, speed, oldProgress, newProgress)</summary>
        public static event Action<Vehicle, int, int, int, int> OnMovementExecuted;
        
        // ==================== POWER EVENTS ====================
        
        /// <summary>Fired when a component shuts down due to insufficient power. Args: (vehicle, component, requiredPower, availablePower)</summary>
        public static event Action<Vehicle, VehicleComponent, int, int> OnComponentPowerShutdown;
        
        // ==================== STAGE EVENTS ====================
        
        /// <summary>Fired when vehicle enters a new stage. Args: (vehicle, newStage, previousStage, carriedProgress, isPlayerChoice)</summary>
        public static event Action<Vehicle, Stage, Stage, int, bool> OnStageEntered;
        
        /// <summary>Fired when vehicle crosses the finish line. Args: (vehicle, finishStage)</summary>
        public static event Action<Vehicle, Stage> OnFinishLineCrossed;
        
        // ==================== PLAYER EVENTS ====================
        
        /// <summary>Fired when player cannot act (vehicle non-operational). Args: (vehicle, reason)</summary>
        public static event Action<Vehicle, string> OnPlayerCannotAct;
        
        /// <summary>Fired when player's action phase begins. Args: vehicle</summary>
        public static event Action<Vehicle> OnPlayerActionPhaseStarted;
        
        /// <summary>Fired when player ends their turn. Args: vehicle</summary>
        public static event Action<Vehicle> OnPlayerEndedTurn;
        
        /// <summary>Fired when player manually triggers movement. Args: vehicle</summary>
        public static event Action<Vehicle> OnPlayerTriggeredMovement;
        
        // ==================== EMIT METHODS - LIFECYCLE ====================
        
        public static void EmitPhaseChanged(TurnPhase oldPhase, TurnPhase newPhase)
            => OnPhaseChanged?.Invoke(oldPhase, newPhase);
        
        public static void EmitRoundStarted(int roundNumber)
            => OnRoundStarted?.Invoke(roundNumber);
        
        public static void EmitRoundEnded(int roundNumber)
            => OnRoundEnded?.Invoke(roundNumber);
        
        public static void EmitTurnStarted(Vehicle vehicle)
            => OnTurnStarted?.Invoke(vehicle);
        
        public static void EmitTurnEnded(Vehicle vehicle)
            => OnTurnEnded?.Invoke(vehicle);
        
        public static void EmitGameOver()
            => OnGameOver?.Invoke();
        
        public static void EmitInitiativeRolled(Vehicle vehicle, int initiative)
            => OnInitiativeRolled?.Invoke(vehicle, initiative);
        
        public static void EmitVehicleRemoved(Vehicle vehicle)
            => OnVehicleRemoved?.Invoke(vehicle);
        
        public static void EmitVehicleDestroyed(Vehicle vehicle)
            => OnVehicleDestroyed?.Invoke(vehicle);
        
        // ==================== EMIT METHODS - OPERATIONS ====================
        
        public static void EmitAutoMovement(Vehicle vehicle)
            => OnAutoMovement?.Invoke(vehicle);
        
        public static void EmitMovementBlocked(Vehicle vehicle, string reason)
            => OnMovementBlocked?.Invoke(vehicle, reason);
        
        public static void EmitMovementExecuted(Vehicle vehicle, int distance, int speed, int oldProgress, int newProgress)
            => OnMovementExecuted?.Invoke(vehicle, distance, speed, oldProgress, newProgress);
        
        public static void EmitComponentPowerShutdown(Vehicle vehicle, VehicleComponent component, int requiredPower, int availablePower)
            => OnComponentPowerShutdown?.Invoke(vehicle, component, requiredPower, availablePower);
        
        public static void EmitStageEntered(Vehicle vehicle, Stage newStage, Stage previousStage, int carriedProgress, bool isPlayerChoice)
            => OnStageEntered?.Invoke(vehicle, newStage, previousStage, carriedProgress, isPlayerChoice);
        
        public static void EmitFinishLineCrossed(Vehicle vehicle, Stage finishStage)
            => OnFinishLineCrossed?.Invoke(vehicle, finishStage);
        
        // ==================== EMIT METHODS - PLAYER ====================
        
        public static void EmitPlayerCannotAct(Vehicle vehicle, string reason)
            => OnPlayerCannotAct?.Invoke(vehicle, reason);
        
        public static void EmitPlayerActionPhaseStarted(Vehicle vehicle)
            => OnPlayerActionPhaseStarted?.Invoke(vehicle);
        
        public static void EmitPlayerEndedTurn(Vehicle vehicle)
            => OnPlayerEndedTurn?.Invoke(vehicle);
        
        public static void EmitPlayerTriggeredMovement(Vehicle vehicle)
            => OnPlayerTriggeredMovement?.Invoke(vehicle);
        
        // ==================== CLEANUP ====================
        
        /// <summary>Clear all subscribers (for testing or scene unload)</summary>
        public static void ClearAllSubscribers()
        {
            // Lifecycle
            OnPhaseChanged = null;
            OnRoundStarted = null;
            OnRoundEnded = null;
            OnTurnStarted = null;
            OnTurnEnded = null;
            OnGameOver = null;
            OnInitiativeRolled = null;
            OnVehicleRemoved = null;
            OnVehicleDestroyed = null;
            
            // Operations
            OnAutoMovement = null;
            OnMovementBlocked = null;
            OnMovementExecuted = null;
            OnComponentPowerShutdown = null;
            OnStageEntered = null;
            OnFinishLineCrossed = null;
            
            // Player
            OnPlayerCannotAct = null;
            OnPlayerActionPhaseStarted = null;
            OnPlayerEndedTurn = null;
            OnPlayerTriggeredMovement = null;
        }
    }
}
