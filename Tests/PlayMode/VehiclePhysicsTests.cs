using NUnit.Framework;
using Assets.Scripts.Entities.Vehicle;

namespace Assets.Scripts.Tests.PlayMode
{
    public class VehiclePhysicsTests
    {
        // ==================== Friction Loss ====================

        [Test]
        public void Physics_FrictionLoss_BasicCalculation()
        {
            // friction=2, drag=10%, speed=50
            // frictionLoss = (baseFriction + dragPercent*speed/100) * 5
            // = (2 + 10*50/100) * 5 = (2 + 5) * 5 = 35
            int result = VehiclePhysicsCalculator.CalculateFrictionLoss(
                currentSpeed: 50, baseFriction: 2, dragCoefficientPercent: 10);

            Assert.AreEqual(35, result, "friction=2, drag=10%, speed=50 should give 35");
        }

        [Test]
        public void Physics_FrictionLoss_ZeroSpeed_OnlyBaseFriction()
        {
            // At rest: (baseFriction + 0) * 5 = 2 * 5 = 10
            int result = VehiclePhysicsCalculator.CalculateFrictionLoss(
                currentSpeed: 0, baseFriction: 2, dragCoefficientPercent: 10);

            Assert.AreEqual(10, result, "At rest, only base friction should apply (* 5)");
        }

        [Test]
        public void Physics_FrictionLoss_HighSpeed_DragDominates()
        {
            // High speed: drag component much larger than base friction
            // (2 + 20*100/100) * 5 = (2 + 20) * 5 = 110
            int result = VehiclePhysicsCalculator.CalculateFrictionLoss(
                currentSpeed: 100, baseFriction: 2, dragCoefficientPercent: 20);

            Assert.AreEqual(110, result, "At high speed, drag should dominate");
            Assert.Greater(result, 50, "High speed friction should be significant");
        }

        // ==================== Power Cost ====================

        [Test]
        public void Physics_PowerCost_IncludesBasePowerAndFriction()
        {
            // basePower=5, friction=2, drag=10%, speed=50
            // frictionCost = 35 (from above)
            // powerCost = max(0, 5 + 35) = 40
            int result = VehiclePhysicsCalculator.CalculateSpeedPowerCost(
                currentSpeed: 50, basePowerDraw: 5, baseFriction: 2, dragCoefficientPercent: 10);

            Assert.AreEqual(40, result, "Power cost should be basePower + frictionLoss");
        }

        [Test]
        public void Physics_PowerCost_NeverNegative()
        {
            // Even with weird inputs, power cost should never go negative
            int result = VehiclePhysicsCalculator.CalculateSpeedPowerCost(
                currentSpeed: 0, basePowerDraw: 0, baseFriction: 0, dragCoefficientPercent: 0);

            Assert.GreaterOrEqual(result, 0, "Power cost should never be negative");
        }
    }
}
