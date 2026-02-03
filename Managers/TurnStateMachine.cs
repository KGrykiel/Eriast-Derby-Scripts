using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Managers.TurnPhases;

namespace Assets.Scripts.Managers
{
    /// <summary>
    /// Turn phases for the state machine.
    /// Follows CRPG-standard round/turn structure (BG3, WOTR pattern).
    /// </summary>
    public enum TurnPhase
    {
        /// <summary>Game not started or combat ended</summary>
        Inactive,
        
        /// <summary>Start of a new round - apply round-start effects</summary>
        RoundStart,
        
        /// <summary>Start of a vehicle's turn - regen power, pay costs, reset flags</summary>
        TurnStart,
        
        /// <summary>Player is selecting actions - WAIT for input</summary>
        PlayerAction,
        
        /// <summary>AI is executing its turn - runs synchronously</summary>
        AIAction,
        
        /// <summary>End of a vehicle's turn - auto-move if needed, tick effects</summary>
        TurnEnd,
        
        /// <summary>End of round - apply round-end effects</summary>
        RoundEnd,
        
        /// <summary>Race/combat is over</summary>
        GameOver
    }

    /// <summary>
    /// State machine for managing turn-based game flow.
    /// Uses Chain of Responsibility pattern - phase handlers process their phase
    /// and return the next phase to transition to.
    /// 
    /// Phase flow:
    /// RoundStart → TurnStart → (PlayerAction | AIAction) → TurnEnd → [next turn or RoundEnd]
    /// RoundEnd → RoundStart (next round)
    /// 
    /// Events are emitted via TurnEventBus for clean decoupling.
    /// </summary>
    public class TurnStateMachine
    {
        // ==================== STATE ====================
        
        private TurnPhase currentPhase = TurnPhase.Inactive;
        private List<Vehicle> vehicles = new();
        private Dictionary<Vehicle, int> initiativeOrder = new();
        private int currentTurnIndex = 0;
        private int currentRound = 0;
        
        // Phase handlers (Chain of Responsibility)
        private Dictionary<TurnPhase, ITurnPhaseHandler> phaseHandlers = new();
        
        // ==================== PUBLIC PROPERTIES ====================
        
        /// <summary>Current phase of the turn system</summary>
        public TurnPhase CurrentPhase => currentPhase;
        
        /// <summary>Current round number (1-indexed)</summary>
        public int CurrentRound => currentRound;
        
        /// <summary>Current turn index within the round (0-indexed)</summary>
        public int CurrentTurnIndex => currentTurnIndex;
        
        /// <summary>Vehicle whose turn it currently is</summary>
        public Vehicle CurrentVehicle => 
            vehicles.Count > 0 && currentTurnIndex < vehicles.Count 
                ? vehicles[currentTurnIndex] 
                : null;
        
        /// <summary>All vehicles in initiative order</summary>
        public IReadOnlyList<Vehicle> AllVehicles => vehicles;
        
        /// <summary>Number of turns remaining in this round (including current)</summary>
        public int TurnsRemainingInRound => vehicles.Count - currentTurnIndex;
        
        /// <summary>Is this the last turn in the round?</summary>
        public bool IsLastTurnInRound => currentTurnIndex >= vehicles.Count - 1;
        
        /// <summary>Is the game active (not inactive or game over)?</summary>
        public bool IsActive => currentPhase != TurnPhase.Inactive && currentPhase != TurnPhase.GameOver;
        
        /// <summary>Is the state machine waiting for player input?</summary>
        public bool IsWaitingForPlayer => currentPhase == TurnPhase.PlayerAction;
        
        // ==================== INITIALIZATION ====================
        
        /// <summary>
        /// Initialize the state machine with vehicles.
        /// Rolls initiative and establishes turn order.
        /// </summary>
        public void Initialize(List<Vehicle> vehicleList)
        {
            vehicles = new List<Vehicle>(vehicleList);
            initiativeOrder.Clear();
            
            // Register phase handlers
            RegisterPhaseHandlers();
            
            // Subscribe to vehicle destruction events
            TurnEventBus.OnVehicleDestroyed += HandleVehicleDestroyed;
            
            // Roll initiative for each vehicle
            foreach (var vehicle in vehicles)
            {
                int initiative = RollUtility.RollInitiative();
                initiativeOrder[vehicle] = initiative;
                TurnEventBus.EmitInitiativeRolled(vehicle, initiative);
            }
            
            // Sort by initiative (descending)
            vehicles.Sort((a, b) => initiativeOrder[b].CompareTo(initiativeOrder[a]));
            
            currentTurnIndex = 0;
            currentRound = 0; // Will become 1 when first round starts
            
            TransitionTo(TurnPhase.RoundStart);
        }
        
        /// <summary>
        /// Handle vehicle destruction event - remove from turn order.
        /// Called via TurnEventBus.OnVehicleDestroyed.
        /// </summary>
        private void HandleVehicleDestroyed(Vehicle vehicle)
        {
            RemoveVehicle(vehicle);
        }
        
        /// <summary>
        /// Register all phase handlers for the Chain of Responsibility.
        /// </summary>
        private void RegisterPhaseHandlers()
        {
            phaseHandlers.Clear();
            
            var handlers = new ITurnPhaseHandler[]
            {
                new RoundStartHandler(),
                new TurnStartHandler(),
                new PlayerActionHandler(),
                new AIActionHandler(),
                new TurnEndHandler(),
                new RoundEndHandler(),
                new GameOverHandler()
            };
            
            foreach (var handler in handlers)
            {
                phaseHandlers[handler.Phase] = handler;
            }
        }
        
        // ==================== PHASE PROCESSING (Chain of Responsibility) ====================
        
        /// <summary>
        /// Process the current phase using the registered handler.
        /// Returns null if execution should pause (waiting for player input or game over).
        /// Returns next phase to transition to otherwise.
        /// </summary>
        public TurnPhase? ProcessCurrentPhase(TurnPhaseContext context)
        {
            if (!phaseHandlers.TryGetValue(currentPhase, out var handler))
            {
                Debug.LogError($"[TurnStateMachine] No handler registered for phase: {currentPhase}");
                return null;
            }
            
            return handler.Execute(context);
        }
        
        /// <summary>
        /// Run the state machine until it hits a pause point (player input or game over).
        /// Call this to start or resume execution.
        /// </summary>
        public void Run(TurnPhaseContext context)
        {
            while (IsActive && !IsWaitingForPlayer && !context.IsGameOver)
            {
                TurnPhase? nextPhase = ProcessCurrentPhase(context);
                
                if (nextPhase.HasValue)
                {
                    TransitionTo(nextPhase.Value);
                }
                else
                {
                    break;  // Paused (waiting for player) or game over
                }
            }
        }
        
        /// <summary>
        /// Resume execution after a pause (e.g., player ended turn).
        /// Transitions to the specified phase and continues running.
        /// </summary>
        public void Resume(TurnPhaseContext context, TurnPhase nextPhase)
        {
            TransitionTo(nextPhase);
            Run(context);
        }
        
        // ==================== STATE TRANSITIONS ====================
        
        /// <summary>
        /// Transition to a new phase. Emits events via TurnEventBus.
        /// </summary>
        public void TransitionTo(TurnPhase newPhase)
        {
            if (currentPhase == newPhase) return;
            
            TurnPhase oldPhase = currentPhase;
            currentPhase = newPhase;
            
            Debug.Log($"[TurnStateMachine] {oldPhase} → {newPhase}");
            
            TurnEventBus.EmitPhaseChanged(oldPhase, newPhase);
            
            // Execute phase-specific entry logic (emits events)
            ExecutePhaseEntry(newPhase);
        }
        
        /// <summary>
        /// Phase entry logic - emits events via TurnEventBus for each phase.
        /// </summary>
        private void ExecutePhaseEntry(TurnPhase phase)
        {
            switch (phase)
            {
                case TurnPhase.RoundStart:
                    currentRound++;
                    TurnEventBus.EmitRoundStarted(currentRound);
                    break;
                    
                case TurnPhase.TurnStart:
                    var vehicle = CurrentVehicle;
                    if (vehicle != null)
                    {
                        TurnEventBus.EmitTurnStarted(vehicle);
                    }
                    break;
                    
                case TurnPhase.TurnEnd:
                    var endingVehicle = CurrentVehicle;
                    if (endingVehicle != null)
                    {
                        TurnEventBus.EmitTurnEnded(endingVehicle);
                    }
                    break;
                    
                case TurnPhase.RoundEnd:
                    TurnEventBus.EmitRoundEnded(currentRound);
                    break;
                    
                case TurnPhase.GameOver:
                    TurnEventBus.EmitGameOver();
                    break;
            }
        }
        
        // ==================== TURN ADVANCEMENT ====================
        
        /// <summary>
        /// Advance to the next turn in the round.
        /// Returns true if we wrapped to a new round.
        /// </summary>
        public bool AdvanceToNextTurn()
        {
            currentTurnIndex++;
            
            if (currentTurnIndex >= vehicles.Count)
            {
                // Round complete
                currentTurnIndex = 0;
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Remove a destroyed vehicle from the turn order.
        /// Adjusts indices to maintain proper flow.
        /// </summary>
        public void RemoveVehicle(Vehicle vehicle)
        {
            int index = vehicles.IndexOf(vehicle);
            if (index < 0) return;
            
            vehicles.RemoveAt(index);
            initiativeOrder.Remove(vehicle);
            
            TurnEventBus.EmitVehicleRemoved(vehicle);
            
            // Adjust current index if needed
            if (index < currentTurnIndex)
            {
                currentTurnIndex--;
            }
            else if (index == currentTurnIndex && currentTurnIndex >= vehicles.Count && vehicles.Count > 0)
            {
                currentTurnIndex = 0;
            }
            
            // Check for game over
            if (vehicles.Count == 0)
            {
                TransitionTo(TurnPhase.GameOver);
            }
        }
        
        /// <summary>
        /// Check if a vehicle should skip its turn (no stage assigned).
        /// Note: Destroyed vehicles are removed via RemoveVehicle, no need to check here.
        /// </summary>
        public bool ShouldSkipTurn(Vehicle vehicle)
        {
            if (vehicle == null) return true;
            if (vehicle.currentStage == null) return true;
            if (vehicle.Status == VehicleStatus.Destroyed) return true;
            return false;
        }
    }
}
