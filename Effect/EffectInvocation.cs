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

        foreach (var target in targets)
        {
            bool apply = true;

            if (requiresRollToHit)
            {
                if (!RollUtility.RollToHit(user as Vehicle, target, rollType, toHitBonus, source?.ToString()))
                {
                    string userName = user is Vehicle vu ? vu.vehicleName : user.name;
                    string targetName = target is Vehicle vt ? vt.vehicleName : target.name;

                    // Debug-level miss logging (detailed analysis only)
                    var userVehicle = user as Vehicle;
                    var targetVehicle = target as Vehicle;

                    RaceHistory.Log(
                        RacingGame.Events.EventType.Combat,
                        EventImportance.Debug, // Downgraded from Medium/Low to Debug
                        $"[MISS] {userName} missed {targetName} (AC check failed)",
                        userVehicle?.currentStage,
                        userVehicle, targetVehicle
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
