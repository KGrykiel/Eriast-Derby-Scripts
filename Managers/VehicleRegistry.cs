using System.Collections.Generic;
using Assets.Scripts.Entities.Vehicles;

namespace Assets.Scripts.Managers
{
    /// <summary>
    /// Static registry for runtime vehicle discovery.
    /// Vehicles self-register on OnEnable and deregister on OnDisable.
    /// Replaces FindObjectsByType usage in GameManager, and supports vehicles spawned at runtime.
    /// </summary>
    public static class VehicleRegistry
    {
        private static readonly List<Vehicle> _vehicles = new();

        public static void Register(Vehicle vehicle)
        {
            if (!_vehicles.Contains(vehicle))
                _vehicles.Add(vehicle);
        }

        public static void Unregister(Vehicle vehicle) => _vehicles.Remove(vehicle);

        /// <summary>Returns a snapshot of all currently registered vehicles.</summary>
        public static List<Vehicle> GetAll() => new(_vehicles);
    }
}
