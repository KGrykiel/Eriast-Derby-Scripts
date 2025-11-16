//using SerializeReferenceEditor;
using System.Collections.Generic;
using UnityEngine;
using RacingGame.Events;

[System.Serializable]
public class EffectInvocation
{
    [SerializeReference]
    public IEffect effect;

    public EffectTargetMode targetMode = EffectTargetMode.Target;
    public bool requiresRollToHit = false;
    public RollType rollType = RollType.ArmorClass;

    /// <summary>
    /// Applies the effect to the appropriate targets.
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
                if (user is Vehicle vehicleUser && vehicleUser.currentStage != null)
                    targets.AddRange(vehicleUser.currentStage.vehiclesInStage.FindAll(v => v != user));
                break;
        }

        // Track if any target was successfully affected
        bool anyApplied = false;
        int missCount = 0;
        List<string> missedTargets = new List<string>();

        foreach (var target in targets)
        {
            bool apply = true;

            if (requiresRollToHit)
            {
                if (!RollUtility.RollToHit(user as Vehicle, target, rollType, toHitBonus, source?.ToString()))
                {
                    string userName = user is Vehicle vu ? vu.vehicleName : user.name;
                    string targetName = target is Vehicle vt ? vt.vehicleName : target.name;

                    // Old logging (keep for backwards compatibility)
                    SimulationLogger.LogEvent($"{userName} missed {targetName} with effect.");

                    // New event logging for misses
                    var userVehicle = user as Vehicle;
                    var targetVehicle = target as Vehicle;

                    EventImportance missImportance = DetermineMissImportance(userVehicle, targetVehicle);

                    RaceHistory.Log(
                        RacingGame.Events.EventType.Combat,
                        missImportance,
                        $"[MISS] {userName} missed {targetName}",
                        userVehicle?.currentStage,
                        userVehicle, targetVehicle
                    ).WithMetadata("missed", true)
                        .WithMetadata("rollType", rollType.ToString())
                        .WithMetadata("toHitBonus", toHitBonus)
                        .WithMetadata("effectType", effect?.GetType().Name ?? "Unknown")
                        .WithMetadata("source", source?.name ?? "unknown");

                    missCount++;
                    missedTargets.Add(targetName);
                    apply = false;
                }
            }

            if (apply)
            {
                effect.Apply(user, target, context, source);
                anyApplied = true; // Mark that at least one target was affected
            }
        }

        // If all targets missed (AoE skill that missed everyone), log a summary
        if (!anyApplied && missCount > 0 && targets.Count > 1)
        {
            var userVehicle = user as Vehicle;
            string userName = userVehicle != null ? userVehicle.vehicleName : user.name;

            EventImportance missImportance = userVehicle != null && userVehicle.controlType == ControlType.Player
                ? EventImportance.Medium
                : EventImportance.Low;

            RaceHistory.Log(
                 RacingGame.Events.EventType.Combat,
                missImportance,
                 $"[MISS] {userName} missed all {missCount} target(s): {string.Join(", ", missedTargets)}",
                userVehicle?.currentStage,
                userVehicle
         ).WithMetadata("missedAll", true)
                .WithMetadata("missCount", missCount)
                .WithMetadata("targetCount", targets.Count)
                .WithMetadata("effectType", effect?.GetType().Name ?? "Unknown");
        }

        return anyApplied;
    }

    /// <summary>
    /// Determines importance of a miss event based on who's involved.
    /// </summary>
    private EventImportance DetermineMissImportance(Vehicle attacker, Vehicle target)
    {
        // Player missing is important (they need to know)
        if (attacker != null && attacker.controlType == ControlType.Player)
            return EventImportance.Medium;

        // NPC missing player is important (player needs to know they're being attacked)
        if (target != null && target.controlType == ControlType.Player)
            return EventImportance.Medium;

        // NPC vs NPC miss is low priority
        return EventImportance.Low;
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
