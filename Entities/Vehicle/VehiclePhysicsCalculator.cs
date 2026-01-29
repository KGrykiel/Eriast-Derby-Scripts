using UnityEngine;

namespace Assets.Scripts.Entities.Vehicle
{
    /// <summary>
    /// Calculator for vehicle physics (friction, drag, power consumption).
    /// INTEGER-FIRST DESIGN: All calculations use integer math internally.
    /// 
    /// Physics Model:
    /// - Friction: Constant force (units/turn) - rolling resistance, mechanical friction
    /// - Drag: Speed-dependent force (percentage) - air resistance scales with speed
    /// 
    /// This centralizes physics formulas away from component logic.
    /// Pattern matches StatCalculator - pure calculation utility.
    /// </summary>
    public static class VehiclePhysicsCalculator
    {
        /// <summary>
        /// Calculate friction loss from current speed.
        /// INTEGER-FIRST: Uses integer division, truncates (D&D standard).
        /// 
        /// Formula: frictionLoss = baseFriction + (dragPercent * speed) / 100
        /// 
        /// Example: friction=2, drag=10%, speed=50
        ///   → 2 + (10*50)/100 = 2 + 5 = 7 units lost
        /// 
        /// Physics:
        /// - baseFriction = constant rolling resistance (always loses this amount)
        /// - dragPercent * speed / 100 = air resistance (scales with speed)
        /// </summary>
        /// <param name="currentSpeed">Current speed in units/turn</param>
        /// <param name="baseFriction">Constant friction in units/turn (1-3 typical)</param>
        /// <param name="dragCoefficientPercent">Aerodynamic drag percentage (10 = 0.10 = 10%)</param>
        /// <returns>Integer friction loss to apply</returns>
        public static int CalculateFrictionLoss(int currentSpeed, int baseFriction, int dragCoefficientPercent)
        {
            // Constant friction + speed-dependent drag
            int dragLoss = (dragCoefficientPercent * currentSpeed) / 100;
            return (baseFriction + dragLoss) * 5;
        }
        
        /// <summary>
        /// Calculate power cost from speed and friction.
        /// INTEGER-FIRST: Uses integer division, truncates (D&D standard).
        /// 
        /// Formula: powerCost = basePower + baseFriction + (dragPercent * speed) / 100
        /// Higher speeds = more power consumption due to drag.
        /// </summary>
        /// <param name="currentSpeed">Current speed in units/turn</param>
        /// <param name="basePowerDraw">Base power draw of drive component</param>
        /// <param name="baseFriction">Constant friction in units/turn</param>
        /// <param name="dragCoefficientPercent">Aerodynamic drag percentage (10 = 0.10 = 10%)</param>
        /// <returns>Total power cost</returns>
        public static int CalculateSpeedPowerCost(int currentSpeed, int basePowerDraw, int baseFriction, int dragCoefficientPercent)
        {
            int frictionCost = CalculateFrictionLoss(currentSpeed, baseFriction, dragCoefficientPercent);
            return Mathf.Max(0, basePowerDraw + frictionCost);
        }
    }
}
