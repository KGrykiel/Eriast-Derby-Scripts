using System.Collections.Generic;

namespace Assets.Scripts.Modifiers.DynamicModifiers
{
    /// <summary>
    /// Static evaluator for dynamic modifiers - attributes that affect other attributes.
    /// Called by StatCalculator during attribute calculations.
    /// 
    /// To add a new formula:
    /// 1. Add a new method (e.g., EvaluateMassToMobility)
    /// 2. Add a case to the switch in EvaluateAll()
    /// 3. Done!
    /// 
    /// Current formulas:
    /// - Speed → AC (motion-based evasion)
    /// 
    /// Future formulas:
    /// - Mass → Mobility (clumsiness)
    /// - Mass → Acceleration (sluggishness)
    /// </summary>
    public static class DynamicModifierEvaluator
    {
        // ==================== FORMULA PARAMETERS ====================
        
        /// <summary>
        /// AC bonus per speed unit. Default: 0.1 (10 speed = +1 AC)
        /// </summary>
        private const float SPEED_TO_AC_RATIO = 1f;
        
        // Future formula parameters:
        // private const float MASS_TO_MOBILITY_RATIO = -0.02f;  // 50 mass = -1 Mobility
        // private const float MASS_TO_ACCELERATION_RATIO = -0.01f;  // 100 mass = -1 Acceleration
        
        // ==================== MAIN ENTRY POINT ====================
        
        /// <summary>
        /// Evaluate all dynamic modifiers for a target attribute.
        /// Returns temporary AttributeModifiers that get included in stat breakdown.
        /// 
        /// To disable ALL dynamic modifiers: Comment out the call in StatCalculator.
        /// To disable ONE formula: Comment out the case below.
        /// </summary>
        public static List<AttributeModifier> EvaluateAll(
            Entity entity, 
            Attribute targetAttribute)
        {
            var dynamicModifiers = new List<AttributeModifier>();
            
            // Evaluate each formula based on target attribute
            switch (targetAttribute)
            {
                case Attribute.ArmorClass:
                    EvaluateSpeedToAC(entity, dynamicModifiers);
                    break;
                
                // Future: Add more target attributes here
                // case Attribute.Mobility:
                //     EvaluateMassToMobility(entity, dynamicModifiers);
                //     break;
                //
                // case Attribute.Acceleration:
                //     EvaluateMassToAcceleration(entity, dynamicModifiers);
                //     break;
            }
            
            return dynamicModifiers;
        }
        
        // ==================== INDIVIDUAL FORMULAS ====================
        
        /// <summary>
        /// Speed → AC: Fast-moving vehicles are harder to hit.
        /// Formula: AC bonus = currentSpeed × SPEED_TO_AC_RATIO
        /// 
        /// Uses CURRENT actual speed (how fast vehicle is moving right now),
        /// not maximum potential speed. Stationary vehicles get no bonus!
        /// </summary>
        private static void EvaluateSpeedToAC(
            Entity entity, 
            List<AttributeModifier> modifiers)
        {
            // Get vehicle and drive component
            Vehicle vehicle = EntityHelpers.GetParentVehicle(entity);
            if (vehicle == null) return;
            
            var drive = vehicle.GetDriveComponent();
            if (drive == null) return;
            
            // Get CURRENT speed (actual movement, not max potential)
            float currentSpeed = drive.GetCurrentSpeed();
            
            // Calculate AC bonus based on actual motion
            float acBonus = currentSpeed * SPEED_TO_AC_RATIO;
            
            if (acBonus > 0)
            {
                modifiers.Add(new AttributeModifier(
                    Attribute.ArmorClass,
                    ModifierType.Flat,
                    acBonus,
                    source: drive, // Drive component is the source (for tracking)
                    category: ModifierCategory.Dynamic,
                    displayNameOverride: "Speed -> AC" // Shows "Speed → AC" in tooltips instead of "Drive"
                ));
            }
        }
        
        // ==================== FUTURE FORMULAS (Templates) ====================
        
        // /// <summary>
        // /// Mass → Mobility: Heavy vehicles are clumsy and fail dodge checks.
        // /// Formula: Mobility penalty = -(totalMass × MASS_TO_MOBILITY_RATIO)
        // /// </summary>
        // private static void EvaluateMassToMobility(
        //     Entity entity,
        //     List<AttributeModifier> modifiers)
        // {
        //     Vehicle vehicle = EntityHelpers.GetParentVehicle(entity);
        //     if (vehicle == null) return;
        //     
        //     // Get total mass (will be implemented in size balancing system)
        //     float totalMass = vehicle.totalMass;
        //     
        //     // Calculate mobility penalty
        //     float mobilityPenalty = -(totalMass * MASS_TO_MOBILITY_RATIO);
        //     
        //     if (mobilityPenalty != 0)
        //     {
        //         modifiers.Add(new AttributeModifier(
        //             Attribute.Mobility,
        //             ModifierType.Flat,
        //             mobilityPenalty,
        //             source: vehicle.chassis, // Chassis (mass holder) is the source
        //             category: ModifierCategory.Dynamic
        //         ));
        //     }
        // }
        
        // /// <summary>
        // /// Mass → Acceleration: Heavy vehicles are sluggish and slow to speed up.
        // /// Formula: Acceleration penalty = -(totalMass × MASS_TO_ACCELERATION_RATIO)
        // /// </summary>
        // private static void EvaluateMassToAcceleration(
        //     Entity entity,
        //     List<AttributeModifier> modifiers)
        // {
        //     Vehicle vehicle = EntityHelpers.GetParentVehicle(entity);
        //     if (vehicle == null) return;
        //     
        //     float totalMass = vehicle.totalMass;
        //     float accelerationPenalty = -(totalMass * MASS_TO_ACCELERATION_RATIO);
        //     
        //     if (accelerationPenalty != 0)
        //     {
        //         modifiers.Add(new AttributeModifier(
        //             Attribute.Acceleration,
        //             ModifierType.Flat,
        //             accelerationPenalty,
        //             source: vehicle.chassis,
        //             category: ModifierCategory.Dynamic
        //         ));
        //     }
        // }
    }
}
