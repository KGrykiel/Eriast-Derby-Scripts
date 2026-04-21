using System.Collections.Generic;
using Assets.Scripts.Core;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Entities.Vehicles.VehicleComponents;
using Assets.Scripts.Managers.Race;
using Assets.Scripts.Managers.Selection;
using Assets.Scripts.Stages.Lanes;

namespace Assets.Scripts.Managers.PlayerUI
{
    /// <summary>
    /// Builds display-ready <see cref="SelectionOption{T}"/> lists from domain objects.
    /// Owns all label formatting and interactability logic so
    /// <see cref="TargetSelectionUIController"/> stays dumb.
    /// </summary>
    public static class TargetOptionBuilder
    {
        public static List<SelectionOption<Vehicle>> VehicleOptions(IEnumerable<Vehicle> vehicles)
        {
            var options = new List<SelectionOption<Vehicle>>();
            foreach (var v in vehicles)
            {
                int hp = v.Chassis != null ? v.Chassis.GetCurrentHealth() : 0;
                string label = $"{v.vehicleName} (HP: {hp})";
                options.Add(new SelectionOption<Vehicle>(v, label));
            }
            return options;
        }

        /// <summary>
        /// Builds component options for a vehicle. Pass sourceOnly = true
        /// when targeting the player's own vehicle (no accessibility restrictions).
        /// </summary>
        public static List<SelectionOption<VehicleComponent>> ComponentOptions(Vehicle vehicle, bool sourceOnly)
        {
            var options = new List<SelectionOption<VehicleComponent>>();

            foreach (var component in vehicle.AllComponents)
            {
                if (component == null) continue;

                string label = BuildComponentLabel(vehicle, component, sourceOnly);
                bool interactable = !component.IsDestroyed() &&
                                    (sourceOnly || vehicle.IsComponentAccessible(component));

                options.Add(new SelectionOption<VehicleComponent>(component, label, interactable));
            }

            return options;
        }

        public static List<SelectionOption<StageLane>> LaneOptions(IEnumerable<StageLane> lanes)
        {
            var options = new List<SelectionOption<StageLane>>();
            foreach (var lane in lanes)
            {
                int count = RacePositionTracker.GetVehiclesInLane(lane).Count;
                string label = $"{lane.laneName} ({count} vehicle{(count == 1 ? "" : "s")})";
                options.Add(new SelectionOption<StageLane>(lane, label));
            }
            return options;
        }

        private static string BuildComponentLabel(Vehicle vehicle, VehicleComponent component, bool sourceOnly)
        {
            var (modifiedAC, _, _) = StatCalculator.GatherDefenseValueWithBreakdown(component);
            int modifiedMaxHP = component.GetMaxHealth();
            string core = $"{component.name} (HP: {component.GetCurrentHealth()}/{modifiedMaxHP}, AC: {modifiedAC})";

            if (component.IsDestroyed())
                return $"[X] {core} - DESTROYED";

            if (!sourceOnly && !vehicle.IsComponentAccessible(component))
                return $"[?] {core} - {vehicle.GetInaccessibilityReason(component)}";

            return $"[>] {core}";
        }
    }
}
