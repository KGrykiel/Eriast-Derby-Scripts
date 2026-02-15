using System;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Entities;
using Assets.Scripts.Combat.Damage;

namespace Assets.Scripts.StatusEffects
{
    /// <summary>
    /// Template asset for the flyweight pattern. Runtime instances use AppliedStatusEffect.
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
        public List<ModifierData> modifiers = new();
        
        [Header("Periodic Effects")]
        [Tooltip("Effects that trigger at the start/end of each turn")]
        public List<PeriodicEffectData> periodicEffects = new();
        
        [Header("Behavioral Effects")]
        [Tooltip("Action/movement restrictions and other behavioral changes")]
        public BehavioralEffectData behavioralEffects;
        
        [Header("Custom Behaviors (Optional)")]
        [Tooltip("Advanced: ScriptableObject behaviors for complex/unusual effects")]
        public List<StatusEffectBehavior> customBehaviors = new();
        
        [Header("Duration")]
        [Tooltip("Base duration in turns. -1 = indefinite (permanent until dispelled), 0 = instant, >0 = number of turns")]
        public int baseDuration = -1;
        
        [Header("Targeting Validation")]
        [Tooltip("Entity must have ALL of these features to receive this effect")]
        public EntityFeature requiredFeatures = EntityFeature.None;
        
        [Tooltip("Entity cannot have ANY of these features to receive this effect")]
        public EntityFeature excludedFeatures = EntityFeature.None;
    }

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

    [Serializable]
    public class PeriodicEffectData
    {
        [Tooltip("Type of periodic effect")]
        public PeriodicEffectType type;

        [Header("Damage (used when type = Damage)")]
        [Tooltip("Damage formula and type (only used for Damage periodic effects)")]
        public DamageFormula damageFormula = new() { baseDice = 0, dieSize = 6, bonus = 5, damageType = DamageType.Fire };

        [Header("Restoration (used for Healing / Energy types)")]
        [Tooltip("Flat amount to restore (positive) or drain (negative) per turn")]
        //TODO: Change this to a dice formula for more interesting variability, like damage has. It's the DnD way.
        public int amount = 5;
    }

    public enum PeriodicEffectType
    {
        Damage,         // Deal damage (Burning, Poison, Bleeding)
        Healing,        // Restore HP (Regeneration, Blessed)
        EnergyDrain,    // Drain energy
        EnergyRestore,  // Restore energy
    }

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

    /// <summary>
    /// Catch-all for complex or unique behaviours. Unused for now.
    /// </summary>
    public abstract class StatusEffectBehavior : ScriptableObject
    {
        public abstract void OnApply(Entity target, UnityEngine.Object applier);
        public abstract void OnTick(Entity target, UnityEngine.Object applier);
        public abstract void OnRemove(Entity target, UnityEngine.Object applier);
    }
}
