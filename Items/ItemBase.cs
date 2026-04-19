using UnityEngine;

namespace Assets.Scripts.Items
{
    /// <summary>Abstract base for all consumable item types. Holds only fields shared by both standalone consumables and ammunition.</summary>
    public abstract class ItemBase : ScriptableObject
    {
        [Header("Basic Properties")]
        public string description;

        [Header("Bulk")]
        [Tooltip("Cargo capacity units consumed per charge.")]
        public int bulkPerCharge = 1;
    }
}
