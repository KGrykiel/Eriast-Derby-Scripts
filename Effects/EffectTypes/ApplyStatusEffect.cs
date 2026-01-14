using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Assets.Scripts.StatusEffects;
using StatusEffectTemplate = Assets.Scripts.StatusEffects.StatusEffect;

/// <summary>
/// Effect that applies a StatusEffect to an entity.
/// This is the PRIMARY way skills apply buffs, debuffs, and conditions.
/// 
/// This effect is STATELESS - Entity.ApplyStatusEffect handles logging automatically.
/// 
/// Usage:
/// - Skills: Haste, Bless, Curse, etc.
/// - EventCards: Random boons/banes
/// - Stages: Environmental hazards (Burning, Frozen, etc.)
/// 
/// StatusEffects are VISIBLE (show icon, tooltip) and have DURATION.
/// For PERMANENT stat changes without visual status, use AttributeModifierEffect.
/// </summary>
[System.Serializable]
public class ApplyStatusEffect : EffectBase
{
    [Header("Status Effect")]
    [Tooltip("The StatusEffect asset to apply (create via Racing/Status Effect menu)")]
    public StatusEffectTemplate statusEffect;
    
    /// <summary>
    /// Applies the status effect to the target entity.
    /// Logging is handled automatically by Entity.ApplyStatusEffect().
    /// 
    /// Parameter convention:
    /// - user: The Entity applying the status (for tracking)
    /// - target: The Entity receiving the status effect
    /// - context: Additional context (usually null for status effects)
    /// - source: Skill/EventCard/Stage that triggered this (for logging)
    /// </summary>
    public override void Apply(Entity user, Entity target, UnityEngine.Object context = null, UnityEngine.Object source = null)
    {
        if (statusEffect == null)
        {
            Debug.LogWarning("[ApplyStatusEffect] No status effect assigned!");
            return;
        }
        
        if (target == null)
        {
            Debug.LogWarning("[ApplyStatusEffect] Target is null!");
            return;
        }
        
        // Apply status effect through Entity system
        // Entity.ApplyStatusEffect handles:
        // - Feature validation (requiredFeatures, excludedFeatures)
        // - Stacking rules (better effect/longer duration wins)
        // - Creating AppliedStatusEffect instance
        // - Adding modifiers to target
        // - LOGGING (automatic)
        target.ApplyStatusEffect(statusEffect, source ?? user);
    }
    
    /// <summary>
    /// Get description for UI/logging.
    /// </summary>
    public string GetDescription()
    {
        if (statusEffect == null) return "No effect";
        
        string duration = statusEffect.baseDuration < 0 
            ? "indefinite" 
            : $"{statusEffect.baseDuration} turns";
        
        return $"Apply {statusEffect.effectName} ({duration})";
    }
}

