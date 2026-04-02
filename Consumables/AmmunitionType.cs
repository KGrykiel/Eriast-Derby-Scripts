using Assets.Scripts.Combat.Rolls.RollSpecs;
using SerializeReferenceEditor;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Consumables
{
    /// <summary>Ammunition loaded into a weapon. On a successful hit, onHitNode fires and one charge is consumed.</summary>
    public class AmmunitionType : ConsumableBase
    {
        [Header("Compatibility")]
        [Tooltip("Weapon tags this ammo is compatible with, e.g. [\"Ranged\", \"Ballistic\"].")]
        public List<string> compatibleWith = new();

        [Header("Effect")]
        [SerializeReference, SR]
        [Tooltip("Fires after a successful weapon attack hit.")]
        public RollNode onHitNode;
    }
}
