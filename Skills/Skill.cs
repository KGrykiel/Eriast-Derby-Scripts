using UnityEngine;
using System.Collections.Generic;
using RacingGame.Events;
using EventType = RacingGame.Events.EventType;
using System.Linq;

public abstract class Skill : ScriptableObject
{
    public string description;
    public int energyCost = 1;

    [SerializeField]
    public List<EffectInvocation> effectInvocations = new List<EffectInvocation>();

    /// <summary>
    /// Uses the skill. Applies all effect invocations and logs the results.
    /// </summary>
    /// <param name="user">The vehicle using the skill</param>
    /// <param name="mainTarget">The primary target of the skill</param>
    /// <returns>True if any effect was applied successfully</returns>
    public virtual bool Use(Vehicle user, Vehicle mainTarget)
    {
        // Check for null target
        if (mainTarget == null)
        {
            EventImportance importance = user.controlType == ControlType.Player
                ? EventImportance.Medium
                : EventImportance.Low;

            RaceHistory.Log(
                EventType.SkillUse,
                importance,
                $"{user.vehicleName} attempted to use {name} but there was no valid target",
                user.currentStage,
                user
            ).WithMetadata("skillName", name)
                .WithMetadata("failed", true)
                .WithMetadata("reason", "NoTarget");

            return false;
        }

        // Check for no effects configured
        if (effectInvocations == null || effectInvocations.Count == 0)
        {
            EventImportance importance = user.controlType == ControlType.Player
                ? EventImportance.Medium
                : EventImportance.Low;

            RaceHistory.Log(
                EventType.SkillUse,
                importance,
                $"{user.vehicleName} attempted to use {name} but it has no effects configured",
                user.currentStage,
                user
            ).WithMetadata("skillName", name)
                .WithMetadata("failed", true)
                .WithMetadata("reason", "NoEffects");

            return false;
        }

        bool anyApplied = false;
        int missCount = 0;
        List<string> effectResults = new List<string>();
        List<int> damageDealt = new List<int>(); // Track actual damage

        foreach (var invocation in effectInvocations)
        {
            int toHitBonus = GetCasterToHitBonus(user, invocation.rollType);

            // Track whether this specific invocation succeeded
            bool invocationSuccess = invocation.Apply(user, mainTarget, user.currentStage, this, toHitBonus);

            if (invocationSuccess)
            {
                anyApplied = true;

                // Extract actual damage dealt from DamageEffect
                if (invocation.effect is DamageEffect damageEffect)
                {
                    int actualDamage = damageEffect.LastDamageRolled;
                    damageDealt.Add(actualDamage);
                    
                    string hitType = invocation.requiresRollToHit ? "hit" : "auto-hit";
                    effectResults.Add($"{actualDamage} damage ({hitType})");
                }
                else
                {
                    // Extract effect details for non-damage effects
                    string effectDescription = GetEffectDescription(invocation, mainTarget);
                    if (!string.IsNullOrEmpty(effectDescription))
                    {
                        effectResults.Add(effectDescription);
                    }
                }
            }
            else
            {
                // Effect missed or failed
                missCount++;
            }
        }

        // Log skill usage (SINGLE COMPREHENSIVE EVENT)
        if (anyApplied)
        {
            int totalDamage = damageDealt.Sum();
            EventImportance importance = DetermineSkillImportance(user, mainTarget, totalDamage);

            // Build comprehensive description with status updates
            string description = BuildCombatDescription(user, mainTarget, effectResults, totalDamage);

            // Single consolidated log entry
            var evt = RaceHistory.Log(
                EventType.SkillUse,
                importance,
                description,
                user.currentStage,
                user, mainTarget
            );

            // Add rich metadata
            evt.WithMetadata("skillName", name)
                .WithMetadata("energyCost", energyCost)
                .WithMetadata("effectCount", effectInvocations.Count)
                .WithMetadata("succeeded", true);

            if (totalDamage > 0)
            {
                float maxHealth = mainTarget.GetAttribute(Attribute.MaxHealth);
                evt.WithMetadata("totalDamage", totalDamage)
                   .WithMetadata("targetOldHealth", mainTarget.health + totalDamage)
                   .WithMetadata("targetNewHealth", mainTarget.health)
                   .WithMetadata("targetHealthPercent", (float)mainTarget.health / maxHealth);
                
                // Add status change flags
                float healthPercent = (float)mainTarget.health / maxHealth;
                if (mainTarget.health <= 0)
                {
                    evt.WithMetadata("targetDestroyed", true);
                }
                else if (healthPercent <= 0.25f)
                {
                    evt.WithMetadata("targetCritical", true);
                }
                else if (healthPercent <= 0.5f)
                {
                    evt.WithMetadata("targetBloodied", true);
                }
            }

            if (missCount > 0)
            {
                evt.WithMetadata("partialMiss", true)
                    .WithMetadata("missCount", missCount);
            }
        }
        else
        {
            // Skill completely failed (all effects missed or invalid)
            // Determine why it failed
            string failureReason = missCount > 0 ? "AllEffectsMissed" : "EffectsInvalid";
            string failureDescription = missCount > 0
                ? $"all {missCount} effect(s) missed"
                : "effects could not be applied";

            EventImportance importance = user.controlType == ControlType.Player
                ? EventImportance.Medium
                : EventImportance.Low;

            RaceHistory.Log(
                EventType.SkillUse,
                importance,
                $"{user.vehicleName} used {name} on {mainTarget.vehicleName}, but {failureDescription}",
                user.currentStage,
                user, mainTarget
            ).WithMetadata("skillName", name)
                .WithMetadata("energyCost", energyCost)
                .WithMetadata("effectCount", effectInvocations.Count)
                .WithMetadata("failed", true)
                .WithMetadata("failureReason", failureReason)
                .WithMetadata("missCount", missCount);
        }

        return anyApplied;
    }

    /// <summary>
    /// Determines the importance level of a skill usage event.
    /// </summary>
    private EventImportance DetermineSkillImportance(Vehicle user, Vehicle target, int damageDealt)
    {
        // Player actions are always at least Medium importance
        if (user.controlType == ControlType.Player || target.controlType == ControlType.Player)
        {
            if (damageDealt > 20)
                return EventImportance.High; // Big damage from/to player

            return EventImportance.Medium;
        }

        // NPC vs NPC
        if (damageDealt > 30)
            return EventImportance.High; // Significant damage

        if (damageDealt > 10)
            return EventImportance.Medium;

        return EventImportance.Low;
    }

    /// <summary>
    /// Builds a human-readable description of the skill usage.
    /// </summary>
    private string BuildSkillDescription(Vehicle user, Vehicle target, List<string> effectResults)
    {
        string baseDesc = $"{user.vehicleName} used {name} on {target.vehicleName}";

        if (effectResults.Count > 0)
        {
            baseDesc += ": " + string.Join(", ", effectResults);
        }

        return baseDesc;
    }

    /// <summary>
    /// NEW: Builds comprehensive combat description with damage and effects.
    /// </summary>
    private string BuildCombatDescription(Vehicle user, Vehicle target, List<string> effectResults, int totalDamage)
    {
        string baseDesc = $"{user.vehicleName} used {name} on {target.vehicleName}";

        if (effectResults.Count > 0)
        {
            baseDesc += ": " + string.Join(", ", effectResults);
        }
        
        // Add status updates inline
        if (totalDamage > 0)
        {
            float maxHealth = target.GetAttribute(Attribute.MaxHealth);
            float healthPercent = (float)target.health / maxHealth;
            
            if (target.health <= 0)
            {
                baseDesc += " - DESTROYED!";
            }
            else if (healthPercent <= 0.25f)
            {
                baseDesc += $" ({target.health} HP - CRITICAL!)";
            }
            else if (healthPercent <= 0.5f)
            {
                baseDesc += $" ({target.health} HP - Bloodied)";
            }
            else
            {
                baseDesc += $" ({target.health} HP remaining)";
            }
        }

        return baseDesc;
    }

    /// <summary>
    /// Gets a description of what an effect did (for logging).
    /// </summary>
    private string GetEffectDescription(EffectInvocation invocation, Vehicle target)
    {
        if (invocation.effect == null)
            return "";

        // Damage effect
        if (invocation.effect is DamageEffect damageEffect)
        {
            if (invocation.requiresRollToHit)
            {
                return $"{damageEffect.damageDice}d{damageEffect.damageDieSize}+{damageEffect.damageBonus} damage";
            }
            return $"{damageEffect.damageDice}d{damageEffect.damageDieSize}+{damageEffect.damageBonus} auto-damage";
        }

        // Modifier effect
        if (invocation.effect is AttributeModifierEffect modEffect)
        {
            string durText = modEffect.durationTurns > 0 ? $" for {modEffect.durationTurns} turns" : " (permanent)";
            return $"{modEffect.type} {modEffect.attribute} {modEffect.value:+0;-0}{durText}";
        }

        // Restoration effect
        if (invocation.effect is ResourceRestorationEffect resEffect)
        {
            return $"restore {resEffect.amount} {resEffect.resourceType}";
        }

        // Generic fallback
        return invocation.effect.GetType().Name;
    }

    /// <summary>
    /// Utility: Get caster's to-hit bonus based on roll type.
    /// </summary>
    protected int GetCasterToHitBonus(Vehicle caster, RollType rollType)
    {
        if (rollType == RollType.None)
            return 0; // No bonus for always-hitting skills

        // For now, we assume all vehicles have a to-hit bonus of 0.
        // Future: Could pull from vehicle attributes
        return 0;
    }
}