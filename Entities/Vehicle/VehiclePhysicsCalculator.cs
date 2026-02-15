using UnityEngine;

namespace Assets.Scripts.Entities.Vehicle
{
    /// <summary>
    /// Physics for acceleration system: friction (constant, units/turn) + drag (speed-dependent, percentage).
    /// </summary>
    public static class VehiclePhysicsCalculator
    {
        /// <summary>
        /// frictionLoss = (baseFriction + dragPercent * speed / 100) * 5
        /// </summary>
        public static int CalculateFrictionLoss(int currentSpeed, int baseFriction, int dragCoefficientPercent)
        {
            int dragLoss = (dragCoefficientPercent * currentSpeed) / 100;
            return (baseFriction + dragLoss) * 5;
        }

        /// <summary>
        /// powerCost = basePower + frictionLoss
        /// </summary>
        public static int CalculateSpeedPowerCost(int currentSpeed, int basePowerDraw, int baseFriction, int dragCoefficientPercent)
        {
            int frictionCost = CalculateFrictionLoss(currentSpeed, baseFriction, dragCoefficientPercent);
            return Mathf.Max(0, basePowerDraw + frictionCost);
        }
    }
}
