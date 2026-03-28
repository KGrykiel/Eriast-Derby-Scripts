namespace Assets.Scripts.Conditions.EntityConditions
{
    /// <summary>Runtime instance of a StatusEffect on an entity.</summary>
    public class AppliedEntityCondition : AppliedConditionBase
    {
        public EntityCondition template;

        public override ConditionBase Template => template;

        public AppliedEntityCondition(EntityCondition template, UnityEngine.Object applier)
            : base(template.baseDuration, applier)
        {
            this.template = template;
        }
    }
}
