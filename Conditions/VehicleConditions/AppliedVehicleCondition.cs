namespace Assets.Scripts.Conditions.VehicleConditions
{
    /// <summary>Runtime instance of a VehicleCondition on a Vehicle.</summary>
    public class AppliedVehicleCondition : AppliedConditionBase
    {
        public VehicleCondition template;

        public override ConditionBase Template => template;

        public AppliedVehicleCondition(VehicleCondition template, UnityEngine.Object applier)
            : base(template.baseDuration, applier)
        {
            this.template = template;
        }
    }
}
