using Assets.Scripts.Skills;
using UnityEngine;

namespace Assets.Scripts.Consumables
{
    /// <summary>Base for all standalone consumable items (grenades, potions, charges). Not usable as ammo.</summary>
    public abstract class Consumable : ConsumableBase
    {
        [Header("Skill")]
        [Tooltip("The skill executed when this consumable is used. Defines targeting, action cost, and effect.")]
        public Skill skill;
    }
}
