using System;
using Assets.Scripts.Combat.Rolls.RollSpecs;
using Assets.Scripts.Items;
using Assets.Scripts.Entities.Vehicles;

namespace Assets.Scripts.Skills.Costs
{
    [Serializable]
    public class ConsumableCost : ISkillCost
    {
        public ItemBase template;

        public bool CanPay(Vehicle vehicle)
        {
            return template != null && vehicle.HasChargesFor(template);
        }

        public void Pay(Vehicle vehicle, RollContext ctx)
        {
            vehicle.TrySpendConsumable(template, ctx.CausalSource);
        }

        public string GetDescription() => template != null ? $"1x {template.name}" : "consumable";
    }
}
