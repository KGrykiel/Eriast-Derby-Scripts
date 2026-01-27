using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using EventType = Assets.Scripts.Logging.EventType;
using Assets.Scripts.Logging;
using EventCard = Assets.Scripts.Events.EventCard.EventCard;

public class Stage : MonoBehaviour
{
    public string stageName;
    public float length = 10f;
    public List<Stage> nextStages = new();

    [Header("Race Configuration")]
    [Tooltip("Is this stage a finish line?")]
    public bool isFinishLine = false;

    [Header("Modifiers applied on enter")]
    public List<AttributeModifierEffect> onEnterModifiers = new();

    [Header("Event Cards")]
    public List<EventCard> eventCards = new();

    [Header("Events")]
    public UnityEvent onEnter;
    public UnityEvent onLeave;

    // Track vehicles currently in this stage
    [HideInInspector]
    public List<Vehicle> vehiclesInStage = new();

    private void OnDrawGizmos()
    {
        if (nextStages != null)
        {
            Gizmos.color = Color.cyan;
            foreach (var nextStage in nextStages)
            {
                if (nextStage != null)
                {
                    Gizmos.DrawLine(transform.position, nextStage.transform.position);
                }
            }
        }
    }

    /// <summary>
    /// Draws and triggers a random event card for the vehicle.
    /// </summary>
    public void DrawAndTriggerEventCard(Vehicle vehicle)
    {
        if (eventCards == null || eventCards.Count == 0) return;

        // Draw a random event card
        var card = eventCards[Random.Range(0, eventCards.Count)];
        
        // Log event card trigger
        EventImportance importance = vehicle.controlType == ControlType.Player 
            ? EventImportance.High 
            : EventImportance.Medium;
        
        RaceHistory.Log(
            EventType.EventCard,
            importance,
            $"{vehicle.vehicleName} triggered event card: {card.name} in {stageName}",
            this,
            vehicle
        ).WithMetadata("eventCardName", card.name)
         .WithMetadata("stageName", stageName)
         .WithShortDescription($"{vehicle.vehicleName}: {card.name}");
        
        // Trigger the card effect
        card.Trigger(vehicle, this);
    }

    /// <summary>
    /// Called when a vehicle enters this stage.
    /// Applies modifiers and triggers event cards.
    /// </summary>
    public void TriggerEnter(Vehicle vehicle)
    {
        // Add vehicle to the list if not already present
        if (!vehiclesInStage.Contains(vehicle))
            vehiclesInStage.Add(vehicle);

        // Stage entry logging removed - already handled by TurnController
        // This prevents duplicate "entered stage" events

        // TODO: Convert onEnterModifiers to StatusEffects
        // Stage hazards should apply StatusEffects to components, not direct modifiers
        /*
        // Apply on-enter modifiers
        if (onEnterModifiers != null && onEnterModifiers.Count > 0)
        {
            foreach (var modData in onEnterModifiers)
            {
                if (modData != null)
                {
                    vehicle.AddModifier(modData.ToRuntimeModifier(this));
                    
                    // Modifier logging removed - Vehicle.AddModifier() already logs this
                    // This prevents duplicate modifier events
                }
            }
        }
        */

        // Draw and trigger event card
        DrawAndTriggerEventCard(vehicle);
        
        // Trigger Unity events
        onEnter?.Invoke();
        
        // Check for combat potential
        CheckForCombatPotential(vehicle);
    }

    /// <summary>
    /// Called when a vehicle leaves this stage.
    /// Removes stage-specific modifiers.
    /// </summary>
    public void TriggerLeave(Vehicle vehicle)
    {
        // Remove vehicle from the list
        bool wasPresent = vehiclesInStage.Remove(vehicle);
        
        if (wasPresent)
        {
            // Log stage exit (debug level - not important)
            RaceHistory.Log(
                EventType.Movement,
                EventImportance.Debug,
                $"{vehicle.vehicleName} left {stageName}",
                this,
                vehicle
            ).WithMetadata("stageName", stageName)
             .WithMetadata("vehicleCount", vehiclesInStage.Count);
        }

        // TODO: Remove StatusEffects applied by this stage when leaving
        // Stage hazards should use StatusEffects with stage as applier source
        // vehicle.RemoveStatusEffectsFromSource(this);

        // Trigger Unity events
        onLeave?.Invoke();
    }

    /// <summary>
    /// Checks if multiple vehicles are in the same stage (potential combat).
    /// Logs rivalry/combat potential events.
    /// </summary>
    private void CheckForCombatPotential(Vehicle enteringVehicle)
    {
        var activeVehicles = vehiclesInStage.FindAll(v => v.Status == VehicleStatus.Active);
        
        if (activeVehicles.Count >= 2)
        {
            EventImportance importance = activeVehicles.Exists(v => v.controlType == ControlType.Player)
                ? EventImportance.High
                : EventImportance.Medium;
            
            string vehicleList = string.Join(", ", activeVehicles.ConvertAll(v => v.vehicleName));
            
            RaceHistory.Log(
                EventType.Rivalry,
                importance,
                $"[POWER] {vehicleList} are all in {stageName} - combat possible!",
                this,
                activeVehicles.ToArray()
            ).WithMetadata("vehicleCount", activeVehicles.Count)
             .WithMetadata("stageName", stageName)
             .WithMetadata("hasPlayer", activeVehicles.Exists(v => v.controlType == ControlType.Player))
             .WithShortDescription($"{activeVehicles.Count} vehicles in {stageName}");
        }
    }
}
