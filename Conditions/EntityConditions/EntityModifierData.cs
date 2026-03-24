using System;
using UnityEngine;

namespace Assets.Scripts.Conditions.EntityConditions
{
    [Serializable]
    public class EntityModifierData
    {
        [Tooltip("Entity attribute to modify (e.g., Mobility, ArmorClass)")]
        public Attribute attribute;
        [Tooltip("Flat adds a fixed amount; Multiplier scales the value")]
        public ModifierType type;
        [Tooltip("Amount to modify by (negative for penalties)")]
        public float value;
    }
}
