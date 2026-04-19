using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Entities.Vehicles.VehicleComponents;
using Assets.Scripts.Managers.Logging.Results;
using Assets.Scripts.Stages;

namespace Assets.Scripts.Managers
{
    /// <summary>Base class for all turn-lifecycle events. Used for logging and reactive systems.</summary>
    public abstract class TurnEvent { }

    // ==================== LIFECYCLE ====================

    public class PhaseChangedEvent : TurnEvent
    {
        public TurnPhase OldPhase { get; }
        public TurnPhase NewPhase { get; }
        public PhaseChangedEvent(TurnPhase oldPhase, TurnPhase newPhase) { OldPhase = oldPhase; NewPhase = newPhase; }
    }

    public class RoundStartedEvent : TurnEvent
    {
        public int RoundNumber { get; }
        public RoundStartedEvent(int roundNumber) { RoundNumber = roundNumber; }
    }

    public class RoundEndedEvent : TurnEvent
    {
        public int RoundNumber { get; }
        public RoundEndedEvent(int roundNumber) { RoundNumber = roundNumber; }
    }

    public class TurnStartedEvent : TurnEvent
    {
        public Vehicle Vehicle { get; }
        public TurnStartedEvent(Vehicle vehicle) { Vehicle = vehicle; }
    }

    public class TurnEndedEvent : TurnEvent
    {
        public Vehicle Vehicle { get; }
        public TurnEndedEvent(Vehicle vehicle) { Vehicle = vehicle; }
    }

    public class RaceOverEvent : TurnEvent
    {
        public RaceResult Result { get; }
        public RaceOverEvent(RaceResult result) { Result = result; }
    }

    public class InitiativeRolledEvent : TurnEvent
    {
        public Vehicle Vehicle { get; }
        public int Initiative { get; }
        public InitiativeRolledEvent(Vehicle vehicle, int initiative) { Vehicle = vehicle; Initiative = initiative; }
    }

    public class VehicleRemovedEvent : TurnEvent
    {
        public Vehicle Vehicle { get; }
        public VehicleRemovedEvent(Vehicle vehicle) { Vehicle = vehicle; }
    }

    public class VehicleDestroyedEvent : TurnEvent
    {
        public Vehicle Vehicle { get; }
        public VehicleDestroyedEvent(Vehicle vehicle) { Vehicle = vehicle; }
    }

    public class VehicleFinishedEvent : TurnEvent
    {
        public Vehicle Vehicle { get; }
        public VehicleFinishedEvent(Vehicle vehicle) { Vehicle = vehicle; }
    }

    // ==================== MOVEMENT ====================

    public class AutoMovementEvent : TurnEvent
    {
        public Vehicle Vehicle { get; }
        public AutoMovementEvent(Vehicle vehicle) { Vehicle = vehicle; }
    }

    public class MovementBlockedEvent : TurnEvent
    {
        public Vehicle Vehicle { get; }
        public string Reason { get; }
        public MovementBlockedEvent(Vehicle vehicle, string reason) { Vehicle = vehicle; Reason = reason; }
    }

    public class MovementExecutedEvent : TurnEvent
    {
        public Vehicle Vehicle { get; }
        public int Distance { get; }
        public int Speed { get; }
        public int OldProgress { get; }
        public int NewProgress { get; }
        public MovementExecutedEvent(Vehicle vehicle, int distance, int speed, int oldProgress, int newProgress)
        {
            Vehicle = vehicle; Distance = distance; Speed = speed; OldProgress = oldProgress; NewProgress = newProgress;
        }
    }

    // ==================== POWER ====================

    public class ComponentPowerShutdownEvent : TurnEvent
    {
        public Vehicle Vehicle { get; }
        public VehicleComponent Component { get; }
        public int RequiredPower { get; }
        public int AvailablePower { get; }
        public ComponentPowerShutdownEvent(Vehicle vehicle, VehicleComponent component, int requiredPower, int availablePower)
        {
            Vehicle = vehicle; Component = component; RequiredPower = requiredPower; AvailablePower = availablePower;
        }
    }

    // ==================== STAGE ====================

    public class StageEnteredEvent : TurnEvent
    {
        public Vehicle Vehicle { get; }
        public Stage NewStage { get; }
        public Stage PreviousStage { get; }
        public int CarriedProgress { get; }
        public bool IsPlayerChoice { get; }
        public StageEnteredEvent(Vehicle vehicle, Stage newStage, Stage previousStage, int carriedProgress, bool isPlayerChoice)
        {
            Vehicle = vehicle; NewStage = newStage; PreviousStage = previousStage;
            CarriedProgress = carriedProgress; IsPlayerChoice = isPlayerChoice;
        }
    }

    public class FinishLineCrossedEvent : TurnEvent
    {
        public Vehicle Vehicle { get; }
        public Stage FinishStage { get; }
        public FinishLineCrossedEvent(Vehicle vehicle, Stage finishStage) { Vehicle = vehicle; FinishStage = finishStage; }
    }

    // ==================== PLAYER ====================

    public class PlayerCannotActEvent : TurnEvent
    {
        public Vehicle Vehicle { get; }
        public string Reason { get; }
        public PlayerCannotActEvent(Vehicle vehicle, string reason) { Vehicle = vehicle; Reason = reason; }
    }

    public class PlayerActionPhaseStartedEvent : TurnEvent
    {
        public Vehicle Vehicle { get; }
        public PlayerActionPhaseStartedEvent(Vehicle vehicle) { Vehicle = vehicle; }
    }

    public class PlayerEndedTurnEvent : TurnEvent
    {
        public Vehicle Vehicle { get; }
        public PlayerEndedTurnEvent(Vehicle vehicle) { Vehicle = vehicle; }
    }

    public class PlayerTriggeredMovementEvent : TurnEvent
    {
        public Vehicle Vehicle { get; }
        public PlayerTriggeredMovementEvent(Vehicle vehicle) { Vehicle = vehicle; }
    }
}
