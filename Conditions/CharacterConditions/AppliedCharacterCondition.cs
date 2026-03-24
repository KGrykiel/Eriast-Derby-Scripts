using Assets.Scripts.Characters;
using System.Collections.Generic;

namespace Assets.Scripts.Conditions.CharacterConditions
{
    /// <summary>
    /// Runtime instance of a CharacterCondition on a VehicleSeat.
    /// Stores created modifiers for eager roll gathering, mirroring AppliedStatusEffect.
    /// </summary>
    public class AppliedCharacterCondition : AppliedConditionBase
    {
        public CharacterCondition template;
        public List<CharacterModifier> createdModifiers = new();

        public override ConditionBase Template => template;

        public AppliedCharacterCondition(CharacterCondition template, UnityEngine.Object applier)
            : base(template.baseDuration, applier)
        {
            this.template = template;
        }
    }
}
