using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Managers.TurnPhases;

namespace Assets.Scripts.Managers
{
    /// <summary>CRPG-standard round/turn phases.</summary>
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
    /// Phase flow: RoundStart → TurnStart → (PlayerAction | AIAction) → TurnEnd → [next turn or RoundEnd] → RoundStart
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

        public TurnPhase CurrentPhase => currentPhase;
        public int CurrentRound => currentRound;
        public int CurrentTurnIndex => currentTurnIndex;

        public Vehicle CurrentVehicle => 
            vehicles.Count > 0 && currentTurnIndex < vehicles.Count 
                ? vehicles[currentTurnIndex] 
                : null;

        public IReadOnlyList<Vehicle> AllVehicles => vehicles;
        public int TurnsRemainingInRound => vehicles.Count - currentTurnIndex;
        public bool IsLastTurnInRound => currentTurnIndex >= vehicles.Count - 1;
        public bool IsActive => currentPhase != TurnPhase.Inactive && currentPhase != TurnPhase.GameOver;
        public bool IsWaitingForPlayer => currentPhase == TurnPhase.PlayerAction;
        
        // ==================== INITIALIZATION ====================
        
        public void Initialize(List<Vehicle> vehicleList)
        {
            vehicles = new List<Vehicle>(vehicleList);
            initiativeOrder.Clear();

            RegisterPhaseHandlers();
            TurnEventBus.OnVehicleDestroyed += HandleVehicleDestroyed;

            foreach (var vehicle in vehicles)
            {
                int initiative = RollUtility.RollInitiative();
                initiativeOrder[vehicle] = initiative;
                TurnEventBus.EmitInitiativeRolled(vehicle, initiative);
            }

            vehicles.Sort((a, b) => initiativeOrder[b].CompareTo(initiativeOrder[a]));

            currentTurnIndex = 0;
            currentRound = 0;

            TransitionTo(TurnPhase.RoundStart);
        }

        private void HandleVehicleDestroyed(Vehicle vehicle)
        {
            RemoveVehicle(vehicle);
        }

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
        
        // ==================== PHASE PROCESSING ====================

        /// <summary>Returns null if execution should pause (player input or game over).</summary>
        public TurnPhase? ProcessCurrentPhase(TurnPhaseContext context)
        {
            if (!phaseHandlers.TryGetValue(currentPhase, out var handler))
            {
                Debug.LogError($"[TurnStateMachine] No handler registered for phase: {currentPhase}");
                return null;
            }

            return handler.Execute(context);
        }

        /// <summary>Runs until a pause point (player input or game over).</summary>
        public void Run(TurnPhaseContext context)
        {
            while (IsActive && !IsWaitingForPlayer && !context.IsGameOver)
            {
                TurnPhase? nextPhase = ProcessCurrentPhase(context);

                if (nextPhase.HasValue)
                    TransitionTo(nextPhase.Value);
                else
                    break;
            }
        }

        public void Resume(TurnPhaseContext context, TurnPhase nextPhase)
        {
            TransitionTo(nextPhase);
            Run(context);
        }

        // ==================== STATE TRANSITIONS ====================

        public void TransitionTo(TurnPhase newPhase)
        {
            if (currentPhase == newPhase) return;

            TurnPhase oldPhase = currentPhase;
            currentPhase = newPhase;

            Debug.Log($"[TurnStateMachine] {oldPhase} → {newPhase}");

            TurnEventBus.EmitPhaseChanged(oldPhase, newPhase);
            ExecutePhaseEntry(newPhase);
        }

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

        /// <summary>Returns true if round wrapped.</summary>
        public bool AdvanceToNextTurn()
        {
            currentTurnIndex++;

            if (currentTurnIndex >= vehicles.Count)
            {
                currentTurnIndex = 0;
                return true;
            }

            return false;
        }

        public void RemoveVehicle(Vehicle vehicle)
        {
            int index = vehicles.IndexOf(vehicle);
            if (index < 0) return;

            vehicles.RemoveAt(index);
            initiativeOrder.Remove(vehicle);

            TurnEventBus.EmitVehicleRemoved(vehicle);

            if (index < currentTurnIndex)
                currentTurnIndex--;
            else if (index == currentTurnIndex && currentTurnIndex >= vehicles.Count && vehicles.Count > 0)
                currentTurnIndex = 0;

            if (vehicles.Count == 0)
                TransitionTo(TurnPhase.GameOver);
        }

        public bool ShouldSkipTurn(Vehicle vehicle)
        {
            if (vehicle == null) return true;
            if (vehicle.currentStage == null) return true;
            if (vehicle.Status == VehicleStatus.Destroyed) return true;
            return false;
        }
    }
}
