using SerializeReferenceEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RacingGame.Events;

[System.Serializable]
public class EffectInvocation
{
    [SerializeReference, SR]
    public IEffect effect;

    public EffectTargetMode targetMode = EffectTargetMode.Target;
    public bool requiresRollToHit = false;
    public RollType rollType = RollType.ArmorClass;

    // Store last roll breakdown for inspection
    private RollBreakdown lastRollBreakdown;
    
    /// <summary>
    /// Gets the last attack roll breakdown (if this invocation required a roll to hit).
    /// </summary>
    public RollBreakdown LastRollBreakdown => lastRollBreakdown;

    /// <summary>
    /// Applies the effect to the appropriate targets using modifier list for transparent roll tracking.
    /// </summary>
    public bool Apply(Entity user, Entity mainTarget, Stage context, Object source, List<RollModifier> modifiers)
    {
        if (effect == null) return false;

        List<Entity> targets = BuildTargetList(user, mainTarget, context);

        bool anyApplied = false;
        int missCount = 0;

        foreach (var target in targets)
        {
            bool apply = true;

            if (requiresRollToHit)
            {
                Vehicle attackerVehicle = EntityHelpers.GetParentVehicle(user);
                
                // Use the new breakdown method
                lastRollBreakdown = RollUtility.RollToHitWithBreakdown(
                    attackerVehicle, 
                    target, 
                    rollType, 
                    modifiers, 
                    source?.ToString()
                );
                
                if (lastRollBreakdown.success != true)
                {
                    string userName = EntityHelpers.GetEntityDisplayName(user);
                    string targetName = EntityHelpers.GetEntityDisplayName(target);
                    Vehicle targetVehicle = EntityHelpers.GetParentVehicle(target);

                    var evt = RaceHistory.Log(
                        RacingGame.Events.EventType.Combat,
                        EventImportance.Debug,
                        $"[MISS] {userName} missed {targetName}: {lastRollBreakdown.ToShortString()}",
                        attackerVehicle?.currentStage,
                        attackerVehicle, targetVehicle
                    );
                    
                    // Add full breakdown to metadata
                    evt.WithMetadata("missed", true)
                       .WithMetadata("rollType", rollType.ToString())
                       .WithMetadata("effectType", effect?.GetType().Name ?? "Unknown")
                       .WithMetadata("rollBreakdown", lastRollBreakdown.ToDetailedString());
                    
                    // Add individual modifier info
                    foreach (var kvp in lastRollBreakdown.ToMetadata())
                    {
                        evt.WithMetadata(kvp.Key, kvp.Value);
                    }

                    missCount++;
                    apply = false;
                }
            }

            if (apply)
            {
                effect.Apply(user, target, context, source);
                anyApplied = true;
            }
        }

        return anyApplied;
    }

    /// <summary>
    /// Legacy method for backward compatibility - converts int bonus to modifier list.
    /// </summary>
    public bool Apply(Entity user, Entity mainTarget, Stage context, Object source, int toHitBonus = 0)
    {
        List<RollModifier> modifiers = null;
        if (toHitBonus != 0)
        {
            modifiers = new List<RollModifier>
            {
                new RollModifier("Bonus", toHitBonus, "Unknown Source")
            };
        }
        
        return Apply(user, mainTarget, context, source, modifiers);
    }

    /// <summary>
    /// Build the list of targets based on target mode.
    /// </summary>
    private List<Entity> BuildTargetList(Entity user, Entity mainTarget, Stage context)
    {
        List<Entity> targets = new List<Entity>();
        
        switch (targetMode)
        {
            case EffectTargetMode.User:
                targets.Add(user);
                break;
            case EffectTargetMode.Target:
                targets.Add(mainTarget);
                break;
            case EffectTargetMode.Both:
                targets.Add(user);
                targets.Add(mainTarget);
                break;
            case EffectTargetMode.AllInStage:
                Stage stage = context;
                if (stage == null && user is VehicleComponent userComp && userComp.ParentVehicle != null)
                {
                    stage = userComp.ParentVehicle.currentStage;
                }
                
                if (stage != null && stage.vehiclesInStage != null)
                {
                    Vehicle userVehicle = EntityHelpers.GetParentVehicle(user);
                    foreach (var vehicle in stage.vehiclesInStage)
                    {
                        if (vehicle != userVehicle && vehicle.chassis != null)
                        {
                            targets.Add(vehicle.chassis);
                        }
                    }
                }
                break;
        }
        
        return targets;
    }
}

public enum EffectTargetMode
{
    User,
    Target,
    Both,
    AllInStage
}
