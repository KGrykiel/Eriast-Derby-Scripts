using Assets.Scripts.Consumables;
using UnityEngine;

namespace Assets.Scripts.Skills
{
    /// <summary>
    /// A skill that requires a consumable charge before it can activate.
    /// The consumable is the fuel; all effects remain on the skill itself.
    /// </summary>
    public class ConsumableGatedSkill : Skill
    {
        [Header("Required Consumable")]
        [Tooltip("A charge of this consumable must be available before this skill can be activated. Never null.")]
        public ConsumableBase requiredConsumable;
    }
}
