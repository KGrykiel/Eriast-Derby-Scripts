using System;
using UnityEngine;

namespace Assets.Scripts.Consumables
{
    /// <summary>Runtime wrapper pairing a consumable template with a charge count.</summary>
    [Serializable]
    public class ConsumableStack
    {
        [Tooltip("Reference to a Consumable or AmmunitionType asset.")]
        public ConsumableBase template;

        [Tooltip("How many charges of this consumable the vehicle is carrying.")]
        public int charges = 1;
    }
}
