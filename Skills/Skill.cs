using UnityEngine;
using System.Collections.Generic;
using RacingGame.Events;
using EventType = RacingGame.Events.EventType;

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
        int totalDamageDealt = 0;
        int missCount = 0;
        List<string> effectResults = new List<string>();

        foreach (var invocation in effectInvocations)
        {
            int toHitBonus = GetCasterToHitBonus(user, invocation.rollType);
      
         // Track whether this specific invocation succeeded
     bool invocationSuccess = invocation.Apply(user, mainTarget, user.currentStage, this, toHitBonus);
    
       if (invocationSuccess)
     {
     anyApplied = true;
     
    // Extract effect details for logging
    string effectDescription = GetEffectDescription(invocation, mainTarget);
     if (!string.IsNullOrEmpty(effectDescription))
    {
 effectResults.Add(effectDescription);
    }
     
      // Track damage for metadata
          if (invocation.effect is DamageEffect damageEffect)
    {
        // Note: We'd need to modify DamageEffect to return damage dealt
    // For now, estimate based on dice
  int estimatedDamage = damageEffect.damageDice * (damageEffect.damageDieSize / 2) + damageEffect.damageBonus;
    totalDamageDealt += estimatedDamage;
      }
 }
  else
       {
        // Effect missed or failed
      missCount++;
      }
     }

        // Log skill usage
        if (anyApplied)
  {
      SimulationLogger.LogEvent($"{user.vehicleName} used {name}.");

   // Determine importance based on effect type and target
    EventImportance importance = DetermineSkillImportance(user, mainTarget, totalDamageDealt);
        
   // Build description
          string description = BuildSkillDescription(user, mainTarget, effectResults);
    
  // Log to event system
     var evt = RaceHistory.Log(
     EventType.SkillUse,
      importance,
      description,
   user.currentStage,
     user, mainTarget
     );
    
            // Add metadata
        evt.WithMetadata("skillName", name)
   .WithMetadata("energyCost", energyCost)
       .WithMetadata("effectCount", effectInvocations.Count)
     .WithMetadata("succeeded", true);
      
     if (totalDamageDealt > 0)
      {
        evt.WithMetadata("estimatedDamage", totalDamageDealt);
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
     SimulationLogger.LogEvent($"{user.vehicleName} used {name}, but it had no effect.");
            
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
