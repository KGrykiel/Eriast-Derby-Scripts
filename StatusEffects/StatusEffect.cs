using System;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Entities;

namespace Assets.Scripts.StatusEffects
{
    /// <summary>
    /// ScriptableObject template defining a named status effect (buff, debuff, condition).
    /// Examples: Haste, Burning, Blessed, Stunned, etc.
    /// 
    /// Uses COMPOSITIONAL design - combines multiple effect types:
    /// - Stat modifiers (buffs/debuffs)
    /// - Periodic effects (DoT, HoT, energy drain/restore)
    /// - Behavioral effects (prevents actions, movement)
    /// - Custom behaviors (extensibility for unusual effects)
    /// 
    /// This is the TEMPLATE (asset) - actual instances on entities use AppliedStatusEffect.
    /// Duration tracking happens at the AppliedStatusEffect level, not on individual modifiers.
    /// </summary>
    [CreateAssetMenu(menuName = "Racing/Status Effect", fileName = "New Status Effect")]
    public class StatusEffect : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Display name of this status effect (e.g., 'Haste', 'Burning')")]
        public string effectName;
        
        [Tooltip("Icon for UI display (buff/debuff bar)")]
        public Sprite icon;
        
        [Tooltip("Description shown in tooltips")]
        [TextArea(2, 4)]
        public string description;
        
        [Tooltip("Color tint for UI display")]
        public Color effectColor = Color.white;
        
        [Header("Stat Modifiers")]
        [Tooltip("Stat changes applied while this effect is active")]
        public List<ModifierData> modifiers = new List<ModifierData>();
        
        [Header("Periodic Effects")]
        [Tooltip("Effects that trigger at the start/end of each turn")]
        public List<PeriodicEffectData> periodicEffects = new List<PeriodicEffectData>();
        
        [Header("Behavioral Effects")]
        [Tooltip("Action/movement restrictions and other behavioral changes")]
        public BehavioralEffectData behavioralEffects;
        
        [Header("Custom Behaviors (Optional)")]
        [Tooltip("Advanced: ScriptableObject behaviors for complex/unusual effects")]
        public List<StatusEffectBehavior> customBehaviors = new List<StatusEffectBehavior>();
        
        [Header("Duration")]
        [Tooltip("Base duration in turns. -1 = indefinite (permanent until dispelled), 0 = instant, >0 = number of turns")]
        public int baseDuration = -1;
        
        [Header("Targeting Validation")]
        [Tooltip("Entity must have ALL of these features to receive this effect")]
        public EntityFeature requiredFeatures = EntityFeature.None;
        
        [Tooltip("Entity cannot have ANY of these features to receive this effect")]
        public EntityFeature excludedFeatures = EntityFeature.None;
    }

    // ==================== COMPOSITIONAL DATA CLASSES ====================

    /// <summary>
    /// Defines a single stat modification as part of a StatusEffect.
    /// </summary>
    [Serializable]
    public class ModifierData
    {
        [Tooltip("Which attribute to modify")]
        public Attribute attribute;
        
        [Tooltip("Type of modification (Flat bonus or Percentage)")]
        public ModifierType type;
        
        [Tooltip("Value of modification (e.g., +10 for flat, +50 for 50% increase)")]
        public float value;
    }

    /// <summary>
    /// Defines a periodic effect (DoT, HoT, energy drain, etc.).
    /// Triggered at the start of each turn while the status effect is active.
    /// </summary>
    [Serializable]
    public class PeriodicEffectData
    {
        [Tooltip("Type of periodic effect")]
        public PeriodicEffectType type;
        
        [Tooltip("Value (damage/healing/energy amount)")]
        public int value;
        
        [Tooltip("Damage type (only used if type is Damage)")]
        public DamageType damageType = DamageType.Fire;
    }

    /// <summary>
    /// Types of periodic effects that can trigger each turn.
    /// </summary>
    public enum PeriodicEffectType
    {
        Damage,         // Deal damage (Burning, Poison, Bleeding)
        Healing,        // Restore HP (Regeneration, Blessed)
        EnergyDrain,    // Drain energy
        EnergyRestore,  // Restore energy
    }

    /// <summary>
    /// Defines behavioral restrictions and modifications.
    /// </summary>
    [Serializable]
    public class BehavioralEffectData
    {
        [Tooltip("Prevents the entity from using skills/actions")]
        public bool preventsActions = false;
        
        [Tooltip("Prevents the entity from moving")]
        public bool preventsMovement = false;
        
        [Tooltip("Damage amplification (1.0 = normal, 1.5 = take 50% more damage, 0.5 = take 50% less)")]
        [Range(0f, 3f)]
        public float damageAmplification = 1f;
    }

    // ==================== EXTENSIBILITY: CUSTOM BEHAVIORS ====================

    /// <summary>
    /// Base class for custom status effect behaviors.
    /// Create subclasses of this for unusual/complex effects that don't fit the standard categories.
    /// 
    /// Examples: Teleportation, spawning entities, spreading to nearby targets, etc.
    /// </summary>
    public abstract class StatusEffectBehavior : ScriptableObject
    {
        /// <summary>
        /// Called when the status effect is first applied to an entity.
        /// </summary>
        public abstract void OnApply(Entity target, UnityEngine.Object applier);
        
        /// <summary>
        /// Called at the start of each turn while the effect is active.
        /// </summary>
        public abstract void OnTick(Entity target, UnityEngine.Object applier);
        
        /// <summary>
        /// Called when the status effect is removed (expired, dispelled, or entity destroyed).
        /// </summary>
        public abstract void OnRemove(Entity target, UnityEngine.Object applier);
    }
}
