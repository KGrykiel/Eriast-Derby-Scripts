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
    
    [Header("Component Targeting (Phase 6)")]
    [Tooltip("Can this skill target specific vehicle components?")]
    public bool allowsComponentTargeting = false;
    
    [Tooltip("Target component name (leave empty to target chassis/vehicle)")]
    public string targetComponentName = "";
    
    [Tooltip("Penalty when targeting components (applied to both attack rolls). 0 = no penalty")]
    [Range(0, 10)]
    public int componentTargetingPenalty = 2;

    /// <summary>
    /// Uses the skill without a weapon. For spells and non-weapon abilities.
    /// </summary>
    public virtual bool Use(Vehicle user, Vehicle mainTarget)
    {
        return Use(user, mainTarget, null);
    }

    /// <summary>
    /// Uses the skill with an optional weapon. Applies all effect invocations and logs the results.
    /// Supports component targeting for vehicles.
    /// </summary>
    public virtual bool Use(Vehicle user, Vehicle mainTarget, WeaponComponent weapon)
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
        
        // Check for component targeting
        // If skill allows component targeting and a target component was selected,
        // use the specialized component targeting logic
        if (allowsComponentTargeting && !string.IsNullOrEmpty(targetComponentName))
        {
            return UseComponentTargeted(user, mainTarget, weapon);
        }

        // Standard skill resolution
        // Get entities for the effect invocation (chassis is the primary entity for a vehicle)
        Entity userEntity = user.chassis;
        Entity targetEntity = mainTarget.chassis;
        
        if (userEntity == null || targetEntity == null)
        {
            Debug.LogWarning($"[Skill] {name}: User or target has no chassis!");
            return false;
        }
        
        bool anyApplied = false;
        int missCount = 0;
        List<string> effectResults = new List<string>();
        List<int> damageDealt = new List<int>();

        // Build modifier list for attack rolls
        var modifiers = BuildAttackModifiers(user, weapon);

        foreach (var invocation in effectInvocations)
        {
            // Pass modifiers list (with full source tracking) instead of single int
            bool invocationSuccess = invocation.Apply(userEntity, targetEntity, user.currentStage, weapon, modifiers);

            if (invocationSuccess)
            {
                anyApplied = true;

                // Extract actual damage dealt from DamageEffect
                if (invocation.effect is DamageEffect damageEffect)
                {
                    int actualDamage = damageEffect.LastDamageRolled;
                    damageDealt.Add(actualDamage);
                    
                    string hitType = invocation.requiresRollToHit ? "hit" : "auto-hit";
                    
                    // Include breakdown info in the result
                    if (damageEffect.LastBreakdown != null)
                    {
                        effectResults.Add($"{damageEffect.LastBreakdown.ToShortString()} ({hitType})");
                    }
                    else
                    {
                        effectResults.Add($"{actualDamage} {damageEffect.LastDamageType} damage ({hitType})");
                    }
                }
                else
                {
                    string effectDescription = GetEffectDescription(invocation, mainTarget);
                    if (!string.IsNullOrEmpty(effectDescription))
                    {
                        effectResults.Add(effectDescription);
                    }
                }
            }
            else
            {
                missCount++;
                
                // Include roll breakdown in miss result
                if (invocation.LastRollBreakdown != null)
                {
                    effectResults.Add($"MISS: {invocation.LastRollBreakdown.ToShortString()}");
                }
            }
        }

        // Log skill usage
        LogSkillResult(user, mainTarget, weapon, anyApplied, missCount, effectResults, damageDealt);

        return anyApplied;
    }

    /// <summary>
    /// Build the list of attack modifiers with proper source tracking.
    /// </summary>
    protected virtual List<RollModifier> BuildAttackModifiers(Vehicle user, WeaponComponent weapon)
    {
        var builder = RollUtility.BuildModifiers();
        
        // Add caster/vehicle bonus (future: from character stats)
        int casterBonus = GetCasterToHitBonus(user, RollType.ArmorClass);
        builder.AddIf(casterBonus != 0, "Caster Bonus", casterBonus, user.vehicleName);
        
        // Add weapon attack bonus
        if (weapon != null && weapon.attackBonus != 0)
        {
            builder.Add("Weapon Attack Bonus", weapon.attackBonus, weapon.name);
        }
        
        // Subclasses can override to add more modifiers
        return builder.Build();
    }

    /// <summary>
    /// Log the skill result with full breakdown information.
    /// </summary>
    private void LogSkillResult(Vehicle user, Vehicle target, WeaponComponent weapon, bool anyApplied, int missCount, List<string> effectResults, List<int> damageDealt)
    {
        if (anyApplied)
        {
            int totalDamage = damageDealt.Sum();
            EventImportance importance = DetermineSkillImportance(user, target, totalDamage);
            string description = BuildCombatDescription(user, target, effectResults, totalDamage);

            var evt = RaceHistory.Log(
                EventType.SkillUse,
                importance,
                description,
                user.currentStage,
                user, target
            );

            evt.WithMetadata("skillName", name)
               .WithMetadata("energyCost", energyCost)
               .WithMetadata("effectCount", effectInvocations.Count)
               .WithMetadata("succeeded", true);
            
            if (weapon != null)
            {
                evt.WithMetadata("weaponUsed", weapon.name);
            }

            if (totalDamage > 0)
            {
                float maxHealth = target.GetAttribute(Attribute.MaxHealth);
                evt.WithMetadata("totalDamage", totalDamage)
                   .WithMetadata("targetOldHealth", target.health + totalDamage)
                   .WithMetadata("targetNewHealth", target.health)
                   .WithMetadata("targetHealthPercent", (float)target.health / maxHealth);
                
                float healthPercent = (float)target.health / maxHealth;
                if (target.health <= 0)
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
            string failureReason = missCount > 0 ? "AllEffectsMissed" : "EffectsInvalid";
            string failureDescription = missCount > 0
                ? $"all {missCount} effect(s) missed"
                : "effects could not be applied";

            EventImportance importance = user.controlType == ControlType.Player
                ? EventImportance.Medium
                : EventImportance.Low;

            var evt = RaceHistory.Log(
                EventType.SkillUse,
                importance,
                $"{user.vehicleName} used {name} on {target.vehicleName}, but {failureDescription}",
                user.currentStage,
                user, target
            );
            
            evt.WithMetadata("skillName", name)
               .WithMetadata("energyCost", energyCost)
               .WithMetadata("effectCount", effectInvocations.Count)
               .WithMetadata("failed", true)
               .WithMetadata("failureReason", failureReason)
               .WithMetadata("missCount", missCount);
            
            // Include roll breakdown details for misses
            if (effectResults.Count > 0)
            {
                evt.WithMetadata("rollDetails", string.Join("; ", effectResults));
            }
        }
    }

    /// <summary>
    /// Determines the importance level of a skill usage event.
    /// </summary>
    private EventImportance DetermineSkillImportance(Vehicle user, Vehicle target, int damageDealt)
    {
        if (user.controlType == ControlType.Player || target.controlType == ControlType.Player)
        {
            if (damageDealt > 20)
                return EventImportance.High;
            return EventImportance.Medium;
        }

        if (damageDealt > 30)
            return EventImportance.High;
        if (damageDealt > 10)
            return EventImportance.Medium;

        return EventImportance.Low;
    }

    /// <summary>
    /// Builds comprehensive combat description with damage and effects.
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

        if (invocation.effect is DamageEffect damageEffect)
        {
            return damageEffect.GetDamageDescription();
        }

        if (invocation.effect is AttributeModifierEffect modEffect)
        {
            string durText = modEffect.durationTurns > 0 ? $" for {modEffect.durationTurns} turns" : " (permanent)";
            return $"{modEffect.type} {modEffect.attribute} {modEffect.value:+0;-0}{durText}";
        }

        if (invocation.effect is ResourceRestorationEffect resEffect)
        {
            return $"restore {resEffect.amount} {resEffect.resourceType}";
        }

        return invocation.effect.GetType().Name;
    }

    /// <summary>
    /// Utility: Get caster's to-hit bonus based on roll type.
    /// </summary>
    protected int GetCasterToHitBonus(Vehicle caster, RollType rollType)
    {
        if (rollType == RollType.None)
            return 0;

        // Future: Pull from vehicle/character attributes
        return 0;
    }
    
    /// <summary>
    /// Two-stage component-targeted attack resolution (Phase 6).
    /// </summary>
    private bool UseComponentTargeted(Vehicle user, Vehicle mainTarget, WeaponComponent weapon)
    {
        VehicleComponent targetComponent = mainTarget.AllComponents
            .FirstOrDefault(c => c.name == targetComponentName);

        if (targetComponent == null || targetComponent.isDestroyed)
        {
            RaceHistory.Log(
                EventType.SkillUse,
                EventImportance.Medium,
                $"{user.vehicleName} tried to target {mainTarget.vehicleName}'s {targetComponentName}, but it's unavailable",
                user.currentStage,
                user, mainTarget
            ).WithMetadata("skillName", name)
             .WithMetadata("targetComponent", targetComponentName)
             .WithMetadata("failed", true)
             .WithMetadata("reason", "ComponentUnavailable");
            
            return false;
        }

        if (!mainTarget.IsComponentAccessible(targetComponent))
        {
            string reason = mainTarget.GetInaccessibilityReason(targetComponent);
            RaceHistory.Log(
                EventType.SkillUse,
                EventImportance.Medium,
                $"{user.vehicleName} cannot target {mainTarget.vehicleName}'s {targetComponentName}: {reason}",
                user.currentStage,
                user, mainTarget
            ).WithMetadata("skillName", name)
             .WithMetadata("targetComponent", targetComponentName)
             .WithMetadata("failed", true)
             .WithMetadata("reason", "ComponentInaccessible")
             .WithMetadata("accessibilityReason", reason);
            
            return false;
        }

        // Calculate damage using the first DamageEffect
        DamageBreakdown damageBreakdown = null;
        
        if (effectInvocations != null && effectInvocations.Count > 0)
        {
            var damageEffect = effectInvocations[0].effect as DamageEffect;
            if (damageEffect != null)
            {
                damageBreakdown = damageEffect.formula.ComputeDamageWithBreakdown(weapon);
            }
        }

        if (damageBreakdown == null || damageBreakdown.rawTotal == 0)
        {
            return false;
        }

        // Build modifiers for component targeting
        var modifiers = RollUtility.BuildModifiers()
            .AddIf(weapon != null && weapon.attackBonus != 0, "Weapon Attack Bonus", weapon?.attackBonus ?? 0, weapon?.name)
            .Add("Component Targeting Penalty", -componentTargetingPenalty, name)
            .Build();

        // Stage 1: Roll vs Component AC
        int componentAC = mainTarget.GetComponentAC(targetComponent);
        var roll1 = RollBreakdown.D20(Random.Range(1, 21), RollCategory.Attack);
        foreach (var mod in modifiers) roll1.WithModifier(mod.name, mod.value, mod.source);
        roll1.Against(componentAC, "Component AC");

        if (roll1.success == true)
        {
            // Hit the component!
            DamagePacket packet = DamagePacket.Create(damageBreakdown.rawTotal, damageBreakdown.damageType, user.chassis);
            int resolved = DamageResolver.ResolveDamage(packet, targetComponent);
            targetComponent.TakeDamage(resolved);
            
            var evt = RaceHistory.Log(
                EventType.SkillUse,
                EventImportance.High,
                $"{user.vehicleName} used {name}: hit {mainTarget.vehicleName}'s {targetComponentName} for {damageBreakdown.ToShortString()} ({roll1.ToShortString()})",
                user.currentStage,
                user, mainTarget
            );
            
            evt.WithMetadata("skillName", name)
               .WithMetadata("targetComponent", targetComponentName)
               .WithMetadata("hitComponent", true)
               .WithMetadata("rollBreakdown", roll1.ToDetailedString())
               .WithMetadata("damageBreakdown", damageBreakdown.ToDetailedString());
            
            return true;
        }

        // Stage 2: Missed component, try chassis
        int chassisAC = mainTarget.GetArmorClass();
        var roll2 = RollBreakdown.D20(Random.Range(1, 21), RollCategory.Attack);
        foreach (var mod in modifiers) roll2.WithModifier(mod.name, mod.value, mod.source);
        roll2.Against(chassisAC, "Chassis AC");

        if (roll2.success == true)
        {
            // Hit chassis instead
            DamagePacket packet = DamagePacket.Create(damageBreakdown.rawTotal, damageBreakdown.damageType, user.chassis);
            int resolved = DamageResolver.ResolveDamage(packet, mainTarget.chassis);
            mainTarget.TakeDamage(resolved);
            
            var evt = RaceHistory.Log(
                EventType.SkillUse,
                EventImportance.Medium,
                $"{user.vehicleName} used {name}: missed {targetComponentName}, hit chassis for {damageBreakdown.ToShortString()} ({roll2.ToShortString()})",
                user.currentStage,
                user, mainTarget
            );
            
            evt.WithMetadata("skillName", name)
               .WithMetadata("targetComponent", targetComponentName)
               .WithMetadata("hitChassis", true)
               .WithMetadata("missedComponent", true)
               .WithMetadata("componentRollBreakdown", roll1.ToDetailedString())
               .WithMetadata("chassisRollBreakdown", roll2.ToDetailedString())
               .WithMetadata("damageBreakdown", damageBreakdown.ToDetailedString());
            
            return true;
        }

        // Both rolls missed
        var missEvt = RaceHistory.Log(
            EventType.SkillUse,
            EventImportance.Medium,
            $"{user.vehicleName} used {name}: completely missed {mainTarget.vehicleName} ({roll1.ToShortString()}, {roll2.ToShortString()})",
            user.currentStage,
            user, mainTarget
        );
        
        missEvt.WithMetadata("skillName", name)
               .WithMetadata("targetComponent", targetComponentName)
               .WithMetadata("failed", true)
               .WithMetadata("reason", "BothRollsMissed")
               .WithMetadata("componentRollBreakdown", roll1.ToDetailedString())
               .WithMetadata("chassisRollBreakdown", roll2.ToDetailedString());
        
        return false;
    }
}