using Assets.Scripts.Combat.Restoration;
using SerializeReferenceEditor;
using UnityEngine;

namespace Assets.Scripts.Effects.EffectTypes
{
    /// <summary>
    /// Restores or drains Health/Energy. Works directly on the target entity passed — no routing.
    /// Uses RestorationFormula for dice-based or flat amounts, evaluated by RestorationCalculator.
    /// </summary>
    [System.Serializable]
    [SRName("Resource Restoration")]
    public class ResourceRestorationEffect : EffectBase
    {
        [Header("Restoration Configuration")]
        [Tooltip("Formula defining resource type, dice, and bonus for restoration/drain")]
        public RestorationFormula formula = new();

        public override void Apply(Entity target, EffectContext context)
        {
            int amount = RestorationCalculator.Roll(formula);
            RestorationApplicator.Apply(formula, amount, target, context.SourceEntity);
        }
    }
}

