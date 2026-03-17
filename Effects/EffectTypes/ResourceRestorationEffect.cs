using UnityEngine;
using Assets.Scripts.Effects;
using Assets.Scripts.Combat.Restoration;

/// <summary>
/// Restores or drains Health/Energy. Works directly on the target entity passed — no routing.
/// Uses RestorationFormula for dice-based or flat amounts, evaluated by RestorationCalculator.
/// </summary>
[System.Serializable]
public class ResourceRestorationEffect : EffectBase
{
    [Header("Restoration Configuration")]
    [Tooltip("Formula defining resource type, dice, and bonus for restoration/drain")]
    public RestorationFormula formula = new();

    public override void Apply(Entity target, EffectContext context, Object source = null)
    {
        int amount = RestorationCalculator.Roll(formula);
        RestorationApplicator.Apply(formula, amount, target, context.SourceEntity, source);
    }
}


