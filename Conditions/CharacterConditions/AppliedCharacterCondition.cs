namespace Assets.Scripts.Conditions.CharacterConditions
{
    /// <summary>
    /// Runtime instance of a CharacterCondition on a VehicleSeat.
    /// </summary>
    public class AppliedCharacterCondition : AppliedConditionBase
    {
        public CharacterCondition template;

        public override ConditionBase Template => template;

        public AppliedCharacterCondition(CharacterCondition template, UnityEngine.Object applier)
            : base(template.baseDuration, applier)
        {
            this.template = template;
        }
    }
}
