using UnityEngine;

namespace Assets.Scripts.Entities.Vehicles
{
    /// <summary>
    /// Shared identity asset for a group of allied vehicles.
    /// Vehicles referencing the same VehicleTeam instance are allies.
    /// Vehicles with no team assigned are independent — hostile to everyone.
    /// </summary>
    [CreateAssetMenu(menuName = "Racing/Vehicle Team", fileName = "New Vehicle Team")]
    public class VehicleTeam : ScriptableObject
    {
        public string teamName;
        public Color teamColour = Color.white;
    }
}
