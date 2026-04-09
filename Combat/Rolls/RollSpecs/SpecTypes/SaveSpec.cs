using UnityEngine;
using Assets.Scripts.Characters;
using Assets.Scripts.Combat.Rolls.Advantage;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Entities.Vehicles.VehicleComponents;

namespace Assets.Scripts.Combat.Rolls.RollSpecs.SpecTypes
{
    /// <summary>
    /// What a saving throw tests — either a vehicle attribute or a character attribute.
    /// Right now it handles both with the other being null, but might think of a better system for this if it gets more complicated.
    /// </summary>
    [System.Serializable]
    public abstract class SaveSpec : IRollSpec
    {
        [Header("Difficulty")]
        [Tooltip("Difficulty class to beat.")]
        public int dc = 15;

        [Header("Advantage / Disadvantage")]
        [Tooltip("Advantage or disadvantage granted by this spec. Normal = no grant.")]
        public RollMode grantedMode;

        [Header("Roller")]
        [Tooltip("Who makes this save — the source of the action or its target. Default is Target (target resists the effect).")]
        public RollerSource roller = RollerSource.Target;

        /// <summary>Display-friendly name for logs and tooltips.</summary>
        public abstract string DisplayName { get; }

        // ==================== FACTORIES ====================

        public static VehicleSaveSpec ForVehicle(VehicleCheckAttribute attribute)
        {
            return new VehicleSaveSpec
            {
                vehicleAttribute = attribute
            };
        }

        public static CharacterSaveSpec ForCharacter(
            CharacterAttribute attribute,
            RoleType requiredRole = RoleType.None)
        {
            return new CharacterSaveSpec
            {
                characterAttribute = attribute,
                requiredRole = requiredRole
            };
        }
    }
}