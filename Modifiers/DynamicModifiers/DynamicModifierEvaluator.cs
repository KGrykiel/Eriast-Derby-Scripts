using System.Collections.Generic;

namespace Assets.Scripts.Modifiers.DynamicModifiers
{
    /// <summary>
    /// Dynamic modifiers are calculated on the fly based on the current state of the entity and its context.
    /// Meant to simulate situational bonuses that aren't easily represented as static modifiers. for example vehicles being harder to hit when moving fast.
    /// Modelled as a separate system from static modifiers to avoid excessive complexity in the static modifier system.
    /// </summary>
    public static class DynamicModifierEvaluator
    {
        private const float SPEED_TO_AC_RATIO = 1f; // way too high, just for testing.

        public static List<AttributeModifier> EvaluateAll(
            Entity entity, 
            Attribute targetAttribute)
        {
            var dynamicModifiers = new List<AttributeModifier>();

            switch (targetAttribute)
            {
                case Attribute.ArmorClass:
                    EvaluateSpeedToAC(entity, dynamicModifiers);
                    break;
            }

            return dynamicModifiers;
        }

        /// <summary>Fast-moving vehicles are harder to hit. AC bonus = currentSpeed × ratio.</summary>
        private static void EvaluateSpeedToAC(
            Entity entity, 
            List<AttributeModifier> modifiers)
        {
            Vehicle vehicle = EntityHelpers.GetParentVehicle(entity);
            if (vehicle == null) return;

            var drive = vehicle.GetDriveComponent();
            if (drive == null) return;

            float currentSpeed = drive.GetCurrentSpeed();
            float acBonus = currentSpeed * SPEED_TO_AC_RATIO;

            if (acBonus > 0)
            {
                modifiers.Add(new AttributeModifier(
                    Attribute.ArmorClass,
                    ModifierType.Flat,
                    acBonus,
                    source: drive,
                    category: ModifierCategory.Dynamic,
                    displayNameOverride: "Speed -> AC"
                ));
            }
        }

        // TODO: add more dynamic modifier evaluations here as needed (e.g. speed -> mobility or health -> integrity)
    }
}
