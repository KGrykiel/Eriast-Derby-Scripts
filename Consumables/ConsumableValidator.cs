using Assets.Scripts.Combat.Rolls.RollSpecs;
using Assets.Scripts.Entities.Vehicles;
using UnityEngine;

namespace Assets.Scripts.Consumables
{
    /// <summary>
    /// Validates consumable use for any call site (player, AI, tests).
    /// Mirrors SkillValidator: all non-UI paths go through here.
    /// </summary>
    public static class ConsumableValidator
    {
        public static bool Validate(RollContext ctx, Consumable consumable)
        {
            VehicleSeat seat = ctx.SourceActor?.GetSeat();
            Vehicle vehicle = ctx.SourceActor?.GetVehicle();

            if (vehicle == null)
            {
                Debug.LogWarning($"[ConsumableValidator] No vehicle found in context for {consumable.name}");
                return false;
            }

            if (!CanAccessConsumableType(seat, consumable))
            {
                string seatName = seat != null ? seat.seatName : "<unknown>";
                Debug.LogWarning($"[ConsumableValidator] {seatName} cannot access {consumable.GetType().Name} — missing ConsumableAccess flag");
                return false;
            }

            if (seat != null && !seat.CanSpendAction(consumable.actionCost))
            {
                Debug.LogWarning($"[ConsumableValidator] {seat.seatName} has no {consumable.actionCost} remaining to use {consumable.name}");
                return false;
            }

            return true;
        }

        internal static bool CanAccessConsumableType(VehicleSeat seat, Consumable consumable)
        {
            if (seat == null) return false;

            if (consumable is CombatConsumable)
                return (seat.consumableAccess & ConsumableAccess.Combat) != ConsumableAccess.None;

            if (consumable is UtilityConsumable)
                return (seat.consumableAccess & ConsumableAccess.Utility) != ConsumableAccess.None;

            return false;
        }
    }
}
