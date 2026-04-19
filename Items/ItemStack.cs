using System;
using UnityEngine;

namespace Assets.Scripts.Items
{
    /// <summary>Runtime wrapper pairing a consumable template with a charge count.</summary>
    [Serializable]
    public class ItemStack
    {
        [Tooltip("Reference to a Consumable or AmmunitionType asset.")]
        public ItemBase template;

        [Tooltip("How many charges of this consumable the vehicle is carrying.")]
        public int charges = 1;
    }
}
