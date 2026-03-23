using System;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Entities;
using SerializeReferenceEditor;

namespace Assets.Scripts.Conditions.EntityConditions
{
    /// <summary>
    /// Entity-domain status effect template. Runtime instances use AppliedStatusEffect.
    /// Inherits identity, behavioural, advantage, duration/stacking, and categorisation from ConditionBase.
    /// </summary>
    [CreateAssetMenu(menuName = "Racing/Status Effect", fileName = "New Status Effect")]
    public class EntityCondition : ConditionBase
    {
        [Header("Stat Modifiers")]
        [Tooltip("Entity attribute changes applied while this effect is active")]
        public List<EntityModifierData> modifiers = new();

        [Header("Periodic Effects")]
        [Tooltip("Effects that trigger at the start/end of each turn")]
        [SerializeReference, SR]
        public List<IPeriodicEffect> periodicEffects = new();

        [Header("Targeting Validation")]
        [Tooltip("Entity must have ALL of these features to receive this effect")]
        public EntityFeature requiredFeatures = EntityFeature.None;

        [Tooltip("Entity cannot have ANY of these features to receive this effect")]
        public EntityFeature excludedFeatures = EntityFeature.None;
    }
}
