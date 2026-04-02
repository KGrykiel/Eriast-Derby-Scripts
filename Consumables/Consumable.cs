using Assets.Scripts.Combat.Rolls.RollSpecs;
using Assets.Scripts.Skills;
using SerializeReferenceEditor;
using UnityEngine;

namespace Assets.Scripts.Consumables
{
    /// <summary>Base for all standalone consumable items (grenades, potions, charges). Not usable as ammo.</summary>
    public abstract class Consumable : ConsumableBase
    {
        [Header("Targeting")]
        public TargetingMode targetingMode = TargetingMode.Enemy;

        [Header("Action Economy")]
        [Tooltip("Which action resource using this consumable costs.")]
        public ActionType actionCost = ActionType.Action;

        [Header("Effect")]
        [SerializeReference, SR]
        [Tooltip("The full resolution of this consumable: roll type, DC, success and failure effects, optional chain.")]
        public RollNode onUseNode;
    }
}
