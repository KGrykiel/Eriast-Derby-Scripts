using System;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Combat.Rolls.Advantage;
using SerializeReferenceEditor;

namespace Assets.Scripts.Conditions
{
    /// <summary>
    /// Abstract base for all condition/status effect templates.
    /// Shared fields for identity, behavioural restrictions, advantage grants,
    /// duration/stacking, and categorisation.
    /// </summary>
    public abstract class ConditionBase : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Display name of this condition")]
        public string effectName;

        [Tooltip("Icon for UI display")]
        public Sprite icon;

        [Tooltip("Description shown in tooltips")]
        [TextArea(2, 4)]
        public string description;

        [Tooltip("Colour tint for UI display")]
        public Color effectColor = Color.white;

        [Header("Behavioural Effects")]
        [Tooltip("Action/movement restrictions")]
        public BehavioralEffectData behavioralEffects;

        [Header("Advantage / Disadvantage")]
        [Tooltip("Advantage or disadvantage grants applied while active")]
        public List<AdvantageGrant> advantageGrants = new();

        [Header("Duration")]
        [Tooltip("Base duration in turns. -1 = indefinite, 0 = instant, >0 = turns")]
        public int baseDuration = -1;

        [Header("Stacking Behaviour")]
        [SerializeReference, SR]
        [Tooltip("How this condition behaves when reapplied. Use Stack to allow multiple concurrent instances.")]
        public IStackingBehaviour stackingBehaviour = new RefreshStacking();

        [Header("Categorisation & Removal")]
        [Tooltip("Categories for classification and removal filtering")]
        public ConditionCategory categories = ConditionCategory.None;

        [Tooltip("Automatic removal triggers")]
        public RemovalTrigger removalTriggers = RemovalTrigger.None;
    }

    [Serializable]
    public class BehavioralEffectData
    {
        [Tooltip("Prevents the entity/character from using skills/actions")]
        public bool preventsActions = false;

        [Tooltip("Prevents the entity/character from moving")]
        public bool preventsMovement = false;
    }
}
