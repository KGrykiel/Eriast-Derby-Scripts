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
    /// Two-stage component-targeted attack resolution with Pathfinder-style logging.
    /// Creates separate log entries for: component attack, chassis attack (if needed), and damage.
    /// </summary>
    private bool UseComponentTargeted(Vehicle user, Vehicle mainTarget, WeaponComponent weapon)
    {
        VehicleComponent targetComponent = mainTarget.AllComponents
            .FirstOrDefault(c => c.name == targetComponentName);

        // Get display name once at the start
        string attackerName = GetAttackerDisplayName(user);
        string weaponText = weapon != null ? $" with {weapon.name}" : "";

        if (targetComponent == null || targetComponent.isDestroyed)
        {
            RaceHistory.Log(
                EventType.SkillUse,
                EventImportance.Medium,
                $"{attackerName} tried to target {mainTarget.vehicleName}'s {targetComponentName}, but it's unavailable",
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
                $"{attackerName} cannot target {mainTarget.vehicleName}'s {targetComponentName}: {reason}",
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

        // Build modifiers for component targeting (NO PENALTY - just base bonuses)
        var componentModifiers = RollUtility.BuildModifiers()
            .AddIf(weapon != null && weapon.attackBonus != 0, "Weapon Attack Bonus", weapon?.attackBonus ?? 0, weapon?.name)
            .Build();

        // Build modifiers for chassis fallback (WITH PENALTY)
        var chassisModifiers = RollUtility.BuildModifiers()
            .AddIf(weapon != null && weapon.attackBonus != 0, "Weapon Attack Bonus", weapon?.attackBonus ?? 0, weapon?.name)
            .Add("Component Targeting Penalty", -componentTargetingPenalty, name)
            .Build();

        // Stage 1: Roll vs Component AC (NO PENALTY)
        int componentAC = mainTarget.GetComponentAC(targetComponent);
        var componentRoll = RollBreakdown.D20(Random.Range(1, 21), RollCategory.Attack);
        foreach (var mod in componentModifiers) componentRoll.WithModifier(mod.name, mod.value, mod.source);
        componentRoll.Against(componentAC, "Component AC");

        if (componentRoll.success == true)
        {
            // HIT THE COMPONENT - Log attack with result
            var attackEvt = RaceHistory.Log(
                EventType.Combat,
                EventImportance.High,
                $"{attackerName} attacks {mainTarget.vehicleName}'s {targetComponentName}{weaponText}. <color=#44FF44>Hit</color>",
                user.currentStage,
                user, mainTarget
            );
            
            attackEvt.WithMetadata("skillName", name)
                    .WithMetadata("targetComponent", targetComponentName)
                    .WithMetadata("result", "hit")
                    .WithMetadata("rollBreakdown", componentRoll.ToDetailedString());
            
            // Apply damage
            DamagePacket packet = DamagePacket.Create(damageBreakdown.rawTotal, damageBreakdown.damageType, user.chassis);
            int resolved = DamageResolver.ResolveDamage(packet, targetComponent);
            targetComponent.TakeDamage(resolved);
            
            // Log damage separately
            var damageEvt = RaceHistory.Log(
                EventType.Combat,
                EventImportance.High,
                $"{attackerName} deals <color=#FFA500>{resolved}</color> damage to {mainTarget.vehicleName}'s {targetComponentName}",
                user.currentStage,
                user, mainTarget
            );
            
            damageEvt.WithMetadata("skillName", name)
                    .WithMetadata("targetComponent", targetComponentName)
                    .WithMetadata("damage", resolved)
                    .WithMetadata("damageBreakdown", damageBreakdown.ToDetailedString());
            
            return true;
        }

        // MISSED COMPONENT - Log component attack miss
        var componentMissEvt = RaceHistory.Log(
            EventType.Combat,
            EventImportance.Medium,
            $"{attackerName} attacks {mainTarget.vehicleName}'s {targetComponentName}{weaponText}. <color=#FF4444>Miss</color>",
            user.currentStage,
            user, mainTarget
        );
        
        componentMissEvt.WithMetadata("skillName", name)
                       .WithMetadata("targetComponent", targetComponentName)
                       .WithMetadata("result", "miss")
                       .WithMetadata("rollBreakdown", componentRoll.ToDetailedString());

        // Stage 2: Try chassis (WITH PENALTY)
        int chassisAC = mainTarget.GetArmorClass();
        var chassisRoll = RollBreakdown.D20(Random.Range(1, 21), RollCategory.Attack);
        foreach (var mod in chassisModifiers) chassisRoll.WithModifier(mod.name, mod.value, mod.source);
        chassisRoll.Against(chassisAC, "Chassis AC");

        if (chassisRoll.success == true)
        {
            // HIT CHASSIS - Log chassis attack with result
            var chassisAttackEvt = RaceHistory.Log(
                EventType.Combat,
                EventImportance.Medium,
                $"{attackerName} attacks {mainTarget.vehicleName}'s chassis{weaponText}. <color=#44FF44>Hit</color>",
                user.currentStage,
                user, mainTarget
            );
            
            chassisAttackEvt.WithMetadata("skillName", name)
                           .WithMetadata("targetComponent", "chassis")
                           .WithMetadata("result", "hit")
                           .WithMetadata("rollBreakdown", chassisRoll.ToDetailedString());
            
            // Apply damage
            DamagePacket packet = DamagePacket.Create(damageBreakdown.rawTotal, damageBreakdown.damageType, user.chassis);
            int resolved = DamageResolver.ResolveDamage(packet, mainTarget.chassis);
            mainTarget.TakeDamage(resolved);
            
            // Log damage separately
            var damageEvt = RaceHistory.Log(
                EventType.Combat,
                EventImportance.Medium,
                $"{attackerName} deals <color=#FFA500>{resolved}</color> damage to {mainTarget.vehicleName}'s chassis",
                user.currentStage,
                user, mainTarget
            );
            
            damageEvt.WithMetadata("skillName", name)
                    .WithMetadata("targetComponent", "chassis")
                    .WithMetadata("damage", resolved)
                    .WithMetadata("damageBreakdown", damageBreakdown.ToDetailedString());
            
            return true;
        }

        // MISSED BOTH - Log chassis attack miss
        var chassisMissEvt = RaceHistory.Log(
            EventType.Combat,
            EventImportance.Medium,
            $"{attackerName} attacks {mainTarget.vehicleName}'s chassis{weaponText}. <color=#FF4444>Miss</color>",
            user.currentStage,
            user, mainTarget
        );
        
        chassisMissEvt.WithMetadata("skillName", name)
                     .WithMetadata("targetComponent", "chassis")
                     .WithMetadata("result", "miss")
                     .WithMetadata("rollBreakdown", chassisRoll.ToDetailedString());
        
        return false;
    }

    /// <summary>
    /// Gets display name for attacker in format: "CharacterName (VehicleName)" or just "VehicleName"
    /// </summary>
    private string GetAttackerDisplayName(Vehicle vehicle)
    {
        // TODO: Get character name from vehicle's driver/pilot component
        // For now, just return vehicle name
        // Future: return $"{characterName} ({vehicle.vehicleName})"
        return vehicle.vehicleName;
    }
}