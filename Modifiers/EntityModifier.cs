using Assets.Scripts.Entities;
using System;

namespace Assets.Scripts.Modifiers
{
    /// <summary>
    /// Flat modifiers are additive, Multiplier modifiers are multiplicative.
    /// Application order (D&D standard): base -> all Flat -> all Multiplier.
    /// </summary>
    [Serializable]
    public class EntityAttributeModifier : ModifierBase
    {
        public EntityAttribute Attribute;

        public EntityAttributeModifier(
            EntityAttribute attribute,
            ModifierType type,
            float value,
            string label = ""
        ) : base(type, value, label)
        {
            Attribute = attribute;
        }
    }
}
