using UnityEngine;

namespace Assets.Scripts.Consumables
{
    /// <summary>Abstract base for all consumable item types. Holds only fields shared by both standalone consumables and ammunition.</summary>
    public abstract class ConsumableBase : ScriptableObject
    {
        [Header("Basic Properties")]
        public string description;

        [Header("Bulk")]
        [Tooltip("Cargo capacity units consumed per charge.")]
        public int bulkPerCharge = 1;
    }
}
