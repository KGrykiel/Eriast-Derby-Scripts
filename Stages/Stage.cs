using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Assets.Scripts.Stages.Lanes;
using Assets.Scripts.StatusEffects;
using Assets.Scripts.Combat.SkillChecks;
using Assets.Scripts.Combat.Saves;
using Assets.Scripts.Combat;
using Assets.Scripts.Effects;
using EventCard = Assets.Scripts.Events.EventCard.EventCard;

namespace Assets.Scripts.Stages
{
    /// <summary>
    /// Represents a stage in the race.
    /// Manages vehicles, lanes, event cards, and stage-wide effects.
    /// </summary>
    public class Stage : MonoBehaviour
    {
    // ==================== CONFIGURATION ====================
    
    [Header("Stage Identity")]
    public string stageName;
    
    [Tooltip("Distance to traverse this stage (D&D-style discrete units)")]
    public int length = 100;
    
    [Tooltip("Possible next stages after completing this one")]
    public List<Stage> nextStages = new();
    
    [Tooltip("Is this stage a finish line?")]
    public bool isFinishLine = false;
    
    [Header("Stage Effects")]
    [Tooltip("StatusEffect applied to all vehicle components when entering stage (e.g., sandstorm, freezing winds)")]
    public StatusEffect stageStatusEffect;
    
    [Header("Event Cards")]
    [Tooltip("Random events that can occur in this stage")]
    public List<EventCard> eventCards = new();
    
    [Header("Lane System")]
    [Tooltip("Lanes in this stage (1-5 lanes). Single lane = no multi-lane tactics")]
    public List<StageLane> lanes = new();
    
    [Header("Unity Events")]
    [Tooltip("Triggered when any vehicle enters this stage")]
    public UnityEvent onEnter;
    
    [Tooltip("Triggered when any vehicle leaves this stage")]
    public UnityEvent onLeave;
    
    // ==================== RUNTIME DATA ====================
    
    /// <summary>
    /// All vehicles currently in this stage.
    /// Updated by TriggerEnter/TriggerLeave.
    /// </summary>
    [HideInInspector]
    public List<Vehicle> vehiclesInStage = new();
    
    // ==================== UNITY LIFECYCLE ====================
    
    private void OnDrawGizmos()
    {
        // Draw connections to next stages
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
    
    // ==================== STAGE ENTRY/EXIT ====================
    
    /// <summary>
    /// Called when a vehicle enters this stage.
    /// Applies stage effects, assigns to lane, triggers event card.
    /// </summary>
    public void TriggerEnter(Vehicle vehicle)
    {
        if (vehicle == null) return;
        
        // Add to stage list
        if (!vehiclesInStage.Contains(vehicle))
            vehiclesInStage.Add(vehicle);
        
        // Apply stage StatusEffect to all components
        if (stageStatusEffect != null)
        {
            ApplyStageStatusEffect(vehicle);
        }
        
        // Assign to default lane (if stage has lanes)
        if (lanes != null && lanes.Count > 0)
        {
            AssignVehicleToDefaultLane(vehicle);
        }
        
        // Draw and trigger event card
        DrawAndTriggerEventCard(vehicle);
        
        // Trigger Unity events
        onEnter?.Invoke();
    }
    
    /// <summary>
    /// Called when a vehicle leaves this stage.
    /// Removes stage effects, lane effects, and cleans up references.
    /// </summary>
    public void TriggerLeave(Vehicle vehicle)
    {
        if (vehicle == null) return;
        
        // Remove from stage list
        bool wasPresent = vehiclesInStage.Remove(vehicle);
        
        if (wasPresent)
        {
            // Remove stage StatusEffect from all components
            if (stageStatusEffect != null)
            {
                RemoveStageStatusEffect(vehicle);
            }
            
            // Remove lane StatusEffect and clear lane reference
            StageLane currentLane = GetVehicleLane(vehicle);
            if (currentLane != null)
            {
                if (currentLane.laneStatusEffect != null)
                {
                    RemoveLaneStatusEffect(vehicle, currentLane.laneStatusEffect);
                }
                
                currentLane.vehiclesInLane.Remove(vehicle);
            }
            
            vehicle.currentLane = null;
            
            // Log stage exit
            this.LogStageExit(vehicle, vehiclesInStage.Count);
        }
        
        // Trigger Unity events
        onLeave?.Invoke();
    }
    
    // ==================== STAGE STATUS EFFECTS ====================
    
    /// <summary>
    /// Apply stage StatusEffect to all components of a vehicle.
    /// </summary>
    private void ApplyStageStatusEffect(Vehicle vehicle)
    {
        foreach (var component in vehicle.AllComponents)
        {
            if (component != null)
            {
                component.ApplyStatusEffect(stageStatusEffect, this);
            }
        }
    }
    
    /// <summary>
    /// Remove stage StatusEffect from all components of a vehicle.
    /// Uses source-based removal for efficiency.
    /// </summary>
    private void RemoveStageStatusEffect(Vehicle vehicle)
    {
        foreach (var component in vehicle.AllComponents)
        {
            if (component != null)
            {
                component.RemoveStatusEffectsFromSource(this);
            }
        }
    }
    
    // ==================== EVENT CARD SYSTEM ====================
    
    /// <summary>
    /// Draws and triggers a random event card for the vehicle.
    /// </summary>
    public void DrawAndTriggerEventCard(Vehicle vehicle)
    {
        if (eventCards == null || eventCards.Count == 0) return;
        
        // Draw random card
        var card = eventCards[Random.Range(0, eventCards.Count)];
        
        // Log event
        this.LogEventCardTrigger(vehicle, card.name);
        
        // Trigger card
        card.Trigger(vehicle, this);
    }
    
    // ==================== LANE SYSTEM ====================
    
    /// <summary>
    /// Get the number of lanes in this stage.
    /// </summary>
    public int GetLaneCount()
    {
        return lanes != null && lanes.Count > 0 ? lanes.Count : 1;
    }
    
    /// <summary>
    /// Get the index of a lane within this stage.
    /// Returns -1 if lane is not found.
    /// </summary>
    public int GetLaneIndex(StageLane lane)
    {
        if (lane == null || lanes == null)
            return -1;
        
        return lanes.IndexOf(lane);
    }
    
    /// <summary>
    /// Get a lane by index.
    /// Returns null if index is out of range.
    /// </summary>
    public StageLane GetLaneByIndex(int index)
    {
        if (lanes == null || index < 0 || index >= lanes.Count)
            return null;
        
        return lanes[index];
    }
    
    /// <summary>
    /// Get which lane a vehicle is currently in.
    /// </summary>
    public StageLane GetVehicleLane(Vehicle vehicle)
    {
        if (vehicle == null || lanes == null || lanes.Count == 0)
            return null;
        
        // Check vehicle's current lane reference
        if (vehicle.currentLane != null && lanes.Contains(vehicle.currentLane))
            return vehicle.currentLane;
        
        // Fallback: Search all lanes
        foreach (var lane in lanes)
        {
            if (lane.vehiclesInLane.Contains(vehicle))
                return lane;
        }
        
        return null;
    }
    
    /// <summary>
    /// Assign a vehicle to a specific lane.
    /// Handles StatusEffect application/removal and lane list updates.
    /// </summary>
    public void AssignVehicleToLane(Vehicle vehicle, StageLane targetLane)
    {
        if (vehicle == null || targetLane == null)
            return;
        
        // Validate lane belongs to this stage
        if (!lanes.Contains(targetLane))
        {
            Debug.LogWarning($"Stage.AssignVehicleToLane: Lane {targetLane.laneName} does not belong to stage {stageName}");
            return;
        }
        
        // Get current lane
        StageLane currentLane = GetVehicleLane(vehicle);
        
        // Remove old lane StatusEffect
        if (currentLane != null && currentLane != targetLane && currentLane.laneStatusEffect != null)
        {
            RemoveLaneStatusEffect(vehicle, currentLane.laneStatusEffect);
        }
        
        // Update lane lists
        if (currentLane != null && currentLane != targetLane)
        {
            currentLane.vehiclesInLane.Remove(vehicle);
        }
        
        if (!targetLane.vehiclesInLane.Contains(vehicle))
        {
            targetLane.vehiclesInLane.Add(vehicle);
        }
        
        // Update vehicle reference
        vehicle.currentLane = targetLane;
        
        // Apply new lane StatusEffect
        if (targetLane.laneStatusEffect != null)
        {
            ApplyLaneStatusEffect(vehicle, targetLane.laneStatusEffect);
        }
    }
    
    /// <summary>
    /// Assign a vehicle to the default lane (middle lane).
    /// Called automatically when vehicle enters stage.
    /// </summary>
    public void AssignVehicleToDefaultLane(Vehicle vehicle)
    {
        if (vehicle == null || lanes == null || lanes.Count == 0)
            return;
        
        // Default to middle lane
        int defaultLaneIndex = lanes.Count / 2;
        StageLane defaultLane = lanes[defaultLaneIndex];
        
        AssignVehicleToLane(vehicle, defaultLane);
    }
    
    /// <summary>
    /// Apply lane StatusEffect to all components of a vehicle.
    /// </summary>
    private void ApplyLaneStatusEffect(Vehicle vehicle, StatusEffect laneEffect)
    {
        foreach (var component in vehicle.AllComponents)
        {
            if (component != null)
            {
                component.ApplyStatusEffect(laneEffect, this);
            }
        }
    }
    
    /// <summary>
    /// Remove lane StatusEffect from all components of a vehicle.
    /// Uses template-based removal to avoid removing stage effects.
    /// </summary>
    private void RemoveLaneStatusEffect(Vehicle vehicle, StatusEffect laneEffect)
    {
        if (laneEffect == null) return;
        
        foreach (var component in vehicle.AllComponents)
        {
            if (component != null)
            {
                // Find and remove effects matching this specific lane template
                var appliedEffects = component.GetActiveStatusEffects();
                var matchingEffects = appliedEffects.Where(a => a.template == laneEffect).ToList();
                
                foreach (var applied in matchingEffects)
                {
                    component.RemoveStatusEffect(applied);
                }
            }
        }
    }
    
    // ==================== LANE TURN EFFECTS ====================
    
    /// <summary>
    /// Process turn effects for a vehicle's current lane.
    /// Called by TurnController at the start/end of each turn.
    /// </summary>
    public void ProcessLaneTurnEffects(Vehicle vehicle)
    {
        if (vehicle == null) return;
        
        // Get current lane
        StageLane currentLane = GetVehicleLane(vehicle);
        if (currentLane == null || currentLane.turnEffects == null || currentLane.turnEffects.Count == 0)
            return;
        
        // Process each turn effect
        foreach (var turnEffect in currentLane.turnEffects)
        {
            if (turnEffect == null) continue;
            
            ResolveLaneTurnEffect(vehicle, currentLane, turnEffect);
        }
    }
    
    /// <summary>
    /// Resolve a single lane turn effect for a vehicle.
    /// Handles skill checks, saves, and effect application.
    /// </summary>
    private void ResolveLaneTurnEffect(Vehicle vehicle, StageLane lane, LaneTurnEffect effect)
    {
        // Route based on check type
        switch (effect.checkType)
        {
            case LaneCheckType.None:
                // No check - apply success effects
                ApplyTurnEffects(vehicle, effect.onSuccess);
                this.LogLaneTurnEffect(vehicle, lane, effect, true);
                break;
            
            case LaneCheckType.SkillCheck:
                ResolveSkillCheckTurnEffect(vehicle, lane, effect);
                break;
            
            case LaneCheckType.SavingThrow:
                ResolveSavingThrowTurnEffect(vehicle, lane, effect);
                break;
        }
    }
    
    /// <summary>
    /// Resolve a turn effect that requires a skill check.
    /// </summary>
    private void ResolveSkillCheckTurnEffect(Vehicle vehicle, StageLane lane, LaneTurnEffect effect)
    {
        if (vehicle.chassis == null) return;
        
        // Perform skill check
        var result = SkillCheckCalculator.PerformSkillCheck(
            vehicle.chassis,
            effect.skillCheckType,
            effect.dc
        );
        
        // Emit event for logging
        CombatEventBus.Emit(new SkillCheckEvent(
            result,
            vehicle.chassis,
            this,
            result.Succeeded
        ));
        
        // Apply appropriate effects
        if (result.Succeeded)
        {
            ApplyTurnEffects(vehicle, effect.onSuccess);
        }
        else
        {
            ApplyTurnEffects(vehicle, effect.onFailure);
        }
        
        // Log result
        this.LogLaneTurnEffectWithCheck(vehicle, lane, effect, result);
    }
    
    /// <summary>
    /// Resolve a turn effect that requires a saving throw.
    /// </summary>
    private void ResolveSavingThrowTurnEffect(Vehicle vehicle, StageLane lane, LaneTurnEffect effect)
    {
        if (vehicle.chassis == null) return;
        
        // Perform saving throw
        var result = SaveCalculator.PerformSavingThrow(
            vehicle.chassis,
            effect.saveType,
            effect.dc
        );
        
        // Emit event for logging
        CombatEventBus.EmitSavingThrow(
            result,
            null,
            vehicle.chassis,
            this,
            result.Succeeded,
            "Chassis"
        );
        
        // Apply appropriate effects
        if (result.Succeeded)
        {
            ApplyTurnEffects(vehicle, effect.onSuccess);
        }
        else
        {
            ApplyTurnEffects(vehicle, effect.onFailure);
        }
        
        // Log result
        this.LogLaneTurnEffectWithSave(vehicle, lane, effect, result);
    }
    
    /// <summary>
    /// Apply a list of effects to a vehicle.
    /// </summary>
    private void ApplyTurnEffects(Vehicle vehicle, List<EffectInvocation> effects)
    {
        if (effects == null || effects.Count == 0) return;
        
        foreach (var effectInvocation in effects)
        {
            if (effectInvocation?.effect != null)
            {
                // Apply effect - targeting handled by effect routing
                effectInvocation.effect.Apply(
                    vehicle.chassis,
                    vehicle.chassis,
                    new EffectContext(),
                    this
                );
            }
        }
    }
    }
}




