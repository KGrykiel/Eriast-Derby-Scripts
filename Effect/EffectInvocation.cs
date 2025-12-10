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

    /// <summary>
    /// Applies the effect to the appropriate targets.
    /// NOTE: user and mainTarget are Entities (components ARE entities).
    /// For vehicle-level operations, get the parent vehicle from the component.
    /// </summary>
    /// <returns>True if the effect was successfully applied to at least one target</returns>
    public bool Apply(Entity user, Entity mainTarget, Stage context, Object source, int toHitBonus = 0)
    {
        if (effect == null) return false;

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
                // Get all entities in the same stage
                // Try to get stage from component's parent vehicle, or from context
                Stage stage = context;
                if (stage == null && user is VehicleComponent userComp && userComp.ParentVehicle != null)
                {
                    stage = userComp.ParentVehicle.currentStage;
                }
                
                if (stage != null && stage.vehiclesInStage != null)
                {
                    // Get chassis (primary entity) of each vehicle in stage, excluding user's vehicle
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

        // Track if any target was successfully affected
        bool anyApplied = false;
        int missCount = 0;

        foreach (var target in targets)
        {
            bool apply = true;

            if (requiresRollToHit)
            {
                // Get vehicles for roll utility (if applicable)
                Vehicle attackerVehicle = EntityHelpers.GetParentVehicle(user);
                
                if (!RollUtility.RollToHit(attackerVehicle, target, rollType, toHitBonus, source?.ToString()))
                {
                    string userName = EntityHelpers.GetEntityDisplayName(user);
                    string targetName = EntityHelpers.GetEntityDisplayName(target);

                    // Debug-level miss logging (detailed analysis only)
                    Vehicle targetVehicle = EntityHelpers.GetParentVehicle(target);

                    RaceHistory.Log(
                        RacingGame.Events.EventType.Combat,
                        EventImportance.Debug, // Downgraded from Medium/Low to Debug
                        $"[MISS] {userName} missed {targetName} (AC check failed)",
                        attackerVehicle?.currentStage,
                        attackerVehicle, targetVehicle
                    ).WithMetadata("missed", true)
                        .WithMetadata("rollType", rollType.ToString())
                        .WithMetadata("toHitBonus", toHitBonus)
                        .WithMetadata("effectType", effect?.GetType().Name ?? "Unknown");

                    missCount++;
                    apply = false;
                }
            }

            if (apply)
            {
                effect.Apply(user, target, context, source);
                anyApplied = true; // Mark that at least one target was affected
            }
        }

        // AoE miss summary removed - Skill.Use() handles all miss logging now
        // This prevents duplicate miss events

        return anyApplied;
    }
}

public enum EffectTargetMode
{
    User,
    Target,
    Both,
    AllInStage
    // Extend as needed
}
