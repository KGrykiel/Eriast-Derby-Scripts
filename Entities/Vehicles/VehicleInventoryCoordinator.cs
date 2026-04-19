using System.Collections.Generic;
using Assets.Scripts.Combat;
using Assets.Scripts.Items;
using Assets.Scripts.Entities.Vehicles.VehicleComponents.ComponentTypes;
using Assets.Scripts.Items.Consumables;
using UnityEngine;

namespace Assets.Scripts.Entities.Vehicles
{
    /// <summary>Manages the vehicle's runtime consumable inventory.</summary>
    public class VehicleInventoryCoordinator
    {
        private readonly Vehicle vehicle;

        public VehicleInventoryCoordinator(Vehicle vehicle)
        {
            this.vehicle = vehicle;
        }

        public IReadOnlyList<ItemStack> GetConsumables() => vehicle.inventory;

        public IReadOnlyList<ItemStack> GetAvailableConsumables(VehicleSeat seat)
        {
            var result = new List<ItemStack>();
            foreach (var stack in vehicle.inventory)
            {
                if (stack.template == null) continue;
                Consumable consumable = stack.template as Consumable;
                if (consumable == null) continue;
                if (ConsumableValidator.CanAccessConsumableType(seat, consumable))
                    result.Add(stack);
            }
            return result;
        }

        public bool HasChargesFor(ItemBase template)
        {
            foreach (var stack in vehicle.inventory)
            {
                if (stack.template == template && stack.charges > 0)
                    return true;
            }
            return false;
        }

        public bool TrySpendConsumable(ItemBase template, string causalSource = "")
        {
            if (template == null) return false;
            for (int i = 0; i < vehicle.inventory.Count; i++)
            {
                ItemStack stack = vehicle.inventory[i];
                if (stack.template == template && stack.charges > 0)
                {
                    stack.charges--;
                    int chargesRemaining = stack.charges;
                    if (stack.charges == 0)
                        vehicle.inventory.RemoveAt(i);
                    CombatEventBus.Emit(new ConsumableSpentEvent(template, vehicle, causalSource, chargesRemaining));
                    return true;
                }
            }
            CombatEventBus.Emit(new ConsumableUnavailableEvent(template, vehicle, causalSource));
            return false;
        }

        public void RestoreConsumable(ItemBase template, int amount, string causalSource = "")
        {
            if (template == null || amount <= 0) return;

            int bulkPerCharge = template.bulkPerCharge;
            if (bulkPerCharge > 0)
            {
                int freeCapacity = GetAvailableCapacity();
                amount = Mathf.Min(amount, freeCapacity / bulkPerCharge);
            }
            if (amount <= 0) return;

            foreach (var stack in vehicle.inventory)
            {
                if (stack.template != template) continue;
                stack.charges += amount;
                CombatEventBus.Emit(new ConsumableRestoredEvent(template, vehicle, causalSource, amount, stack.charges));
                return;
            }

            vehicle.inventory.Add(new ItemStack { template = template, charges = amount });
            CombatEventBus.Emit(new ConsumableRestoredEvent(template, vehicle, causalSource, amount, amount));
        }

        public void TrimInventoryToCapacity()
        {
            List<ItemStack> inventory = vehicle.inventory;
            int usedBulk = GetCargoFill();
            int capacity = GetCargoCapacity();

            while (usedBulk > capacity && inventory.Count > 0)
            {
                ItemStack last = inventory[^1];
                if (last.template != null)
                    usedBulk -= last.charges * last.template.bulkPerCharge;
                inventory.RemoveAt(inventory.Count - 1);
            }
        }

        private int GetCargoCapacity()
        {
            ChassisComponent chassis = vehicle.Chassis;
            return chassis != null ? chassis.GetCargoCapacity() : 0;
        }

        private int GetCargoFill()
        {
            int usedBulk = 0;
            foreach (var stack in vehicle.inventory)
            {
                if (stack.template != null)
                    usedBulk += stack.charges * stack.template.bulkPerCharge;
            }
            return usedBulk;
        }

        private int GetAvailableCapacity() => Mathf.Max(0, GetCargoCapacity() - GetCargoFill());
    }
}
