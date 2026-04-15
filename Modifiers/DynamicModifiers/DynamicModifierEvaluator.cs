using Assets.Scripts.Entities;
using Assets.Scripts.Entities.Vehicles;
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
        private const float SPEED_TO_AC_RATIO = 0.1f;

        public static List<EntityAttributeModifier> EvaluateAll(
            Entity entity, 
            EntityAttribute targetAttribute)
        {
            var dynamicModifiers = new List<EntityAttributeModifier>();

            switch (targetAttribute)
            {
                case EntityAttribute.ArmorClass:
                    EvaluateSpeedToAC(entity, dynamicModifiers);
                    break;
            }

            return dynamicModifiers;
        }

        /// <summary>Fast-moving vehicles are harder to hit. AC bonus = currentSpeed × ratio.</summary>
        private static void EvaluateSpeedToAC(
            Entity entity, 
            List<EntityAttributeModifier> modifiers)
        {
            Vehicle vehicle = EntityHelpers.GetParentVehicle(entity);
            if (vehicle == null) return;

            var drive = vehicle.Drive;
            if (drive == null) return;

            float currentSpeed = drive.GetCurrentSpeed();
            int acBonus = (int)(currentSpeed * SPEED_TO_AC_RATIO);

            if (acBonus > 0)
            {
                modifiers.Add(new EntityAttributeModifier(
                    EntityAttribute.ArmorClass,
                    ModifierType.Flat,
                    acBonus,
                    "Speed -> AC"
                ));
            }
        }

        // TODO: add more dynamic modifier evaluations here as needed (e.g. speed -> mobility or health -> integrity)
    }
}
