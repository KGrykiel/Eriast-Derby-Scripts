using System;
using System.Collections.Generic;
using UnityEngine;

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
    /// Separates WHAT (phases) from HOW (GameManager orchestration).
    /// 
    /// Phase flow:
    /// RoundStart → TurnStart → (PlayerAction | AIAction) → TurnEnd → [next turn or RoundEnd]
    /// RoundEnd → RoundStart (next round)
    /// 
    /// Events allow clean hooking without tight coupling.
    /// </summary>
    public class TurnStateMachine
    {
        // ==================== STATE ====================
        
        private TurnPhase currentPhase = TurnPhase.Inactive;
        private List<Vehicle> vehicles = new();
        private Dictionary<Vehicle, int> initiativeOrder = new();
        private int currentTurnIndex = 0;
        private int currentRound = 0;
        
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
        
        // ==================== EVENTS ====================
        
        /// <summary>Fired when phase changes. Args: (oldPhase, newPhase)</summary>
        public event Action<TurnPhase, TurnPhase> OnPhaseChanged;
        
        /// <summary>Fired at start of each round. Args: roundNumber</summary>
        public event Action<int> OnRoundStarted;
        
        /// <summary>Fired at end of each round. Args: roundNumber</summary>
        public event Action<int> OnRoundEnded;
        
        /// <summary>Fired at start of a vehicle's turn. Args: vehicle</summary>
        public event Action<Vehicle> OnTurnStarted;
        
        /// <summary>Fired at end of a vehicle's turn. Args: vehicle</summary>
        public event Action<Vehicle> OnTurnEnded;
        
        /// <summary>Fired when game ends</summary>
        public event Action OnGameOver;
        
        /// <summary>Fired when initiative is rolled. Args: (vehicle, initiativeValue)</summary>
        public event Action<Vehicle, int> OnInitiativeRolled;
        
        /// <summary>Fired when a vehicle is removed from turn order. Args: vehicle</summary>
        public event Action<Vehicle> OnVehicleRemoved;
        
        // ==================== INITIALIZATION ====================
        
        
        /// <summary>
        /// Initialize the state machine with vehicles.
        /// Rolls initiative and establishes turn order.
        /// </summary>
        public void Initialize(List<Vehicle> vehicleList)
        {
            vehicles = new List<Vehicle>(vehicleList);
            initiativeOrder.Clear();
            
            // Roll initiative for each vehicle
            foreach (var vehicle in vehicles)
            {
                int initiative = RollUtility.RollInitiative();
                initiativeOrder[vehicle] = initiative;
                OnInitiativeRolled?.Invoke(vehicle, initiative);
            }
            
            // Sort by initiative (descending)
            vehicles.Sort((a, b) => initiativeOrder[b].CompareTo(initiativeOrder[a]));
            
            currentTurnIndex = 0;
            currentRound = 0; // Will become 1 when first round starts
            
            TransitionTo(TurnPhase.RoundStart);
        }
        
        // ==================== STATE TRANSITIONS ====================
        
        /// <summary>
        /// Transition to a new phase. Fires events.
        /// </summary>
        public void TransitionTo(TurnPhase newPhase)
        {
            if (currentPhase == newPhase) return;
            
            TurnPhase oldPhase = currentPhase;
            currentPhase = newPhase;
            
            Debug.Log($"[TurnStateMachine] {oldPhase} → {newPhase}");
            
            OnPhaseChanged?.Invoke(oldPhase, newPhase);
            
            // Execute phase-specific entry logic
            ExecutePhaseEntry(newPhase);
        }
        
        /// <summary>
        /// Phase entry logic - fires events for each phase.
        /// Logging is handled by TurnEventLogger which subscribes to these events.
        /// </summary>
        private void ExecutePhaseEntry(TurnPhase phase)
        {
            switch (phase)
            {
                case TurnPhase.RoundStart:
                    currentRound++;
                    OnRoundStarted?.Invoke(currentRound);
                    break;
                    
                case TurnPhase.TurnStart:
                    var vehicle = CurrentVehicle;
                    if (vehicle != null)
                    {
                        OnTurnStarted?.Invoke(vehicle);
                    }
                    break;
                    
                case TurnPhase.TurnEnd:
                    var endingVehicle = CurrentVehicle;
                    if (endingVehicle != null)
                    {
                        OnTurnEnded?.Invoke(endingVehicle);
                    }
                    break;
                    
                case TurnPhase.RoundEnd:
                    OnRoundEnded?.Invoke(currentRound);
                    break;
                    
                case TurnPhase.GameOver:
                    OnGameOver?.Invoke();
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
            
            OnVehicleRemoved?.Invoke(vehicle);
            
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
        
        /// <summary>
        /// Get initiative value for a vehicle.
        /// </summary>
        public int GetInitiative(Vehicle vehicle)
        {
            return initiativeOrder.TryGetValue(vehicle, out int init) ? init : 0;
        }
    }
}
