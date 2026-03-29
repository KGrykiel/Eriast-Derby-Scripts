using UnityEngine;
using SerializeReferenceEditor;
using Assets.Scripts.Entities.Vehicles;

namespace Assets.Scripts.Combat.Rolls.RollSpecs.SpecTypes
{
    [System.Serializable]
    [SRName("Vehicle Save")]
    public sealed class VehicleSaveSpec : SaveSpec
    {
        [Tooltip("Vehicle attribute to save against.")]
        public VehicleCheckAttribute vehicleAttribute;

        public override string DisplayName => vehicleAttribute.ToString();
    }
}
