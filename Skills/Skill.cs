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
    /// <param name="user">The vehicle using the skill</param>
    /// <param name="mainTarget">The primary target of the skill</param>
    /// <param name="weapon">Optional weapon component (for weapon-based skills)</param>
    /// <returns>True if any effect was applied successfully</returns>
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

        foreach (var invocation in effectInvocations)
        {
            int toHitBonus = GetCasterToHitBonus(user, invocation.rollType);
            
            // Add weapon's attack bonus if present
            if (weapon != null)
            {
                toHitBonus += weapon.attackBonus;
            }

            // Pass weapon as source so DamageEffect can use it
            bool invocationSuccess = invocation.Apply(userEntity, targetEntity, user.currentStage, weapon, toHitBonus);

            if (invocationSuccess)
            {
                anyApplied = true;

                // Extract actual damage dealt from DamageEffect
                if (invocation.effect is DamageEffect damageEffect)
                {
                    int actualDamage = damageEffect.LastDamageRolled;
                    damageDealt.Add(actualDamage);
                    
                    string hitType = invocation.requiresRollToHit ? "hit" : "auto-hit";
                    effectResults.Add($"{actualDamage} {damageEffect.LastDamageType} damage ({hitType})");
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
            
            if (weapon != null)
            {
                evt.WithMetadata("weaponUsed", weapon.name);
            }

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
                return EventImportance.High;

            return EventImportance.Medium;
        }

        // NPC vs NPC
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

        // Damage effect - use formula description
        if (invocation.effect is DamageEffect damageEffect)
        {
            return damageEffect.GetDamageDescription();
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
            return 0;

        // Future: Could pull from vehicle/character attributes
        return 0;
    }
    
    /// <summary>
    /// Two-stage component-targeted attack resolution (Phase 6).
    /// Stage 1: Roll vs Component AC (with penalty)
    /// Stage 2: If miss, roll vs Chassis AC (with penalty)
    /// </summary>
    private bool UseComponentTargeted(Vehicle user, Vehicle mainTarget, WeaponComponent weapon)
    {
        // Find target component
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

        // Check if component is accessible
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

        // Calculate damage using the first DamageEffect (if any)
        int damage = 0;
        DamageType damageType = DamageType.Physical;
        
        if (effectInvocations != null && effectInvocations.Count > 0)
        {
            var damageEffect = effectInvocations[0].effect as DamageEffect;
            if (damageEffect != null)
            {
                damage = damageEffect.formula.ComputeDamage(weapon, out damageType);
            }
        }

        if (damage == 0)
        {
            return false;
        }

        // Calculate attack bonus
        int attackBonus = weapon?.attackBonus ?? 0;

        // Stage 1: Roll vs Component AC
        int componentAC = mainTarget.GetComponentAC(targetComponent);
        int roll1 = Random.Range(1, 21);
        int total1 = roll1 + attackBonus - componentTargetingPenalty;

        if (total1 >= componentAC)
        {
            // Hit the component!
            DamagePacket packet = DamagePacket.Create(damage, damageType, user.chassis);
            int resolved = DamageResolver.ResolveDamage(packet, targetComponent);
            targetComponent.TakeDamage(resolved);
            
            RaceHistory.Log(
                EventType.SkillUse,
                EventImportance.High,
                $"{user.vehicleName} used {name}: hit {mainTarget.vehicleName}'s {targetComponentName} for {resolved} {damageType} damage (rolled {roll1}+{attackBonus}-{componentTargetingPenalty}={total1} vs AC {componentAC})",
                user.currentStage,
                user, mainTarget
            ).WithMetadata("skillName", name)
             .WithMetadata("targetComponent", targetComponentName)
             .WithMetadata("damage", resolved)
             .WithMetadata("damageType", damageType.ToString())
             .WithMetadata("hitComponent", true)
             .WithMetadata("roll", roll1)
             .WithMetadata("penalty", componentTargetingPenalty)
             .WithMetadata("totalRoll", total1)
             .WithMetadata("targetAC", componentAC);
            
            return true;
        }

        // Stage 2: Missed component, try chassis
        int chassisAC = mainTarget.GetArmorClass();
        int roll2 = Random.Range(1, 21);
        int total2 = roll2 + attackBonus - componentTargetingPenalty;

        if (total2 >= chassisAC)
        {
            // Hit chassis instead
            DamagePacket packet = DamagePacket.Create(damage, damageType, user.chassis);
            int resolved = DamageResolver.ResolveDamage(packet, mainTarget.chassis);
            mainTarget.TakeDamage(resolved);
            
            RaceHistory.Log(
                EventType.SkillUse,
                EventImportance.Medium,
                $"{user.vehicleName} used {name}: missed {targetComponentName}, hit {mainTarget.vehicleName}'s chassis for {resolved} {damageType} damage (rolled {roll2}+{attackBonus}-{componentTargetingPenalty}={total2} vs AC {chassisAC})",
                user.currentStage,
                user, mainTarget
            ).WithMetadata("skillName", name)
             .WithMetadata("targetComponent", targetComponentName)
             .WithMetadata("damage", resolved)
             .WithMetadata("damageType", damageType.ToString())
             .WithMetadata("hitChassis", true)
             .WithMetadata("missedComponent", true)
             .WithMetadata("roll", roll2)
             .WithMetadata("penalty", componentTargetingPenalty)
             .WithMetadata("totalRoll", total2)
             .WithMetadata("targetAC", chassisAC);
            
            return true;
        }

        // Both rolls missed
        RaceHistory.Log(
            EventType.SkillUse,
            EventImportance.Medium,
            $"{user.vehicleName} used {name}: completely missed {mainTarget.vehicleName} (component roll: {total1} vs {componentAC}, chassis roll: {total2} vs {chassisAC})",
            user.currentStage,
            user, mainTarget
        ).WithMetadata("skillName", name)
         .WithMetadata("targetComponent", targetComponentName)
         .WithMetadata("failed", true)
         .WithMetadata("reason", "BothRollsMissed")
         .WithMetadata("componentRoll", total1)
         .WithMetadata("componentAC", componentAC)
         .WithMetadata("chassisRoll", total2)
         .WithMetadata("chassisAC", chassisAC);
        
        return false;
    }
}