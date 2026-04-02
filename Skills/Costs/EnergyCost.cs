using System;
using Assets.Scripts.Combat.Rolls.RollSpecs;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Entities.Vehicles.VehicleComponents;

namespace Assets.Scripts.Skills.Costs
{
    [Serializable]
    public class EnergyCost : ISkillCost
    {
        public int amount;

        public bool CanPay(Vehicle vehicle)
        {
            return vehicle.PowerCore != null && vehicle.PowerCore.CanDrawPower(amount, null);
        }

        public void Pay(Vehicle vehicle, RollContext ctx)
        {
            VehicleComponent sourceComponent = ctx.SourceActor?.GetEntity() as VehicleComponent;
            vehicle.PowerCore.DrawPower(amount, sourceComponent, ctx.CausalSource);
        }

        public string GetDescription() => $"{amount} EN";
    }
}
