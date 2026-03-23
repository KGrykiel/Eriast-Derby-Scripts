using System.Collections.Generic;

namespace Assets.Scripts.Conditions.EntityConditions
{
    /// <summary>Runtime instance of a StatusEffect on an entity. Extends AppliedCondition with entity-specific modifier tracking.</summary>
    public class AppliedEntityCondition : AppliedConditionBase
    {
        public EntityCondition template;
        public List<AttributeModifier> createdModifiers = new();

        public override ConditionBase Template => template;

        public AppliedEntityCondition(EntityCondition template, UnityEngine.Object applier)
            : base(template.baseDuration, applier)
        {
            this.template = template;
        }
    }
}
