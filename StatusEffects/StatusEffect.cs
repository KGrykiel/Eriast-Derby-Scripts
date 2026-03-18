using System;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Entities;
using Assets.Scripts.Combat.Damage;
using Assets.Scripts.Combat.Restoration;
using Assets.Scripts.Combat.Rolls.Advantage;
using SerializeReferenceEditor;

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
        [SerializeReference, SR]
        public List<IPeriodicEffect> periodicEffects = new();
        
        [Header("Behavioral Effects")]
        [Tooltip("Action/movement restrictions and other behavioral changes")]
        public BehavioralEffectData behavioralEffects;
        
        [Header("Custom Behaviors (Optional)")]
        [Tooltip("Advanced: ScriptableObject behaviors for complex/unusual effects")]
        public List<StatusEffectBehavior> customBehaviors = new();

        [Header("Advantage / Disadvantage")]
        [Tooltip("Advantage or disadvantage grants applied while this effect is active")]
        public List<AdvantageGrant> advantageGrants = new();

        [Header("Duration")]
        [Tooltip("Base duration in turns. -1 = indefinite (permanent until dispelled), 0 = instant, >0 = number of turns")]
        public int baseDuration = -1;

        [Header("Stacking Behaviour")]
        [Tooltip("How this effect behaves when reapplied: Refresh (reset duration), Stack (multiple instances), Ignore (no change), Replace (if stronger)")]
        public StackBehaviour stackBehaviour = StackBehaviour.Refresh;

        [Tooltip("Maximum number of stacks allowed. ONLY USED IF stackBehaviour = Stack. 0 = unlimited stacks.")]
        public int maxStacks = 0;

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

    public interface IPeriodicEffect { }

    [Serializable]
    [SRName("Damage (DoT)")]
    public class PeriodicDamageEffect : IPeriodicEffect
    {
        [Tooltip("Damage formula and type")]
        public DamageFormula damageFormula = new() { baseDice = 1, dieSize = 6, bonus = 0, damageType = DamageType.Fire };
    }

    [Serializable]
    [SRName("Restoration (HoT/Energy)")]
    public class PeriodicRestorationEffect : IPeriodicEffect
    {
        [Tooltip("Restoration formula defining resource type, dice, and bonus")]
        public RestorationFormula formula = new();
    }

    [Serializable]
    public class BehavioralEffectData
    {
        [Tooltip("Prevents the entity from using skills/actions")]
        public bool preventsActions = false;

        [Tooltip("Prevents the entity from moving")]
        public bool preventsMovement = false;
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
