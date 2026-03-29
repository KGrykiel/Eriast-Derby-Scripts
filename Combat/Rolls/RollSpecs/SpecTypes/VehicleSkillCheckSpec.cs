using UnityEngine;
using SerializeReferenceEditor;
using Assets.Scripts.Entities.Vehicles;

namespace Assets.Scripts.Combat.Rolls.RollSpecs.SpecTypes
{
    [System.Serializable]
    [SRName("Vehicle Check")]
    public sealed class VehicleSkillCheckSpec : SkillCheckSpec
    {
        [Tooltip("Vehicle attribute to check.")]
        public VehicleCheckAttribute vehicleAttribute;

        public override string DisplayName => vehicleAttribute.ToString();
    }
}
