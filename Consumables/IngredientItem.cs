using UnityEngine;

namespace Assets.Scripts.Consumables
{
    /// <summary>
    /// A purely passive resource item — has no action, no targeting, no roll node.
    /// Used exclusively as a cost ingredient for skills via <see cref="Assets.Scripts.Skills.Costs.ConsumableCost"/>.
    /// Example: a rare diamond required to cast Resurrection.
    /// Owning an IngredientItem grants no action on its own.
    /// </summary>
    [CreateAssetMenu(fileName = "New Ingredient", menuName = "Eriast Derby/Consumables/Ingredient Item")]
    public class IngredientItem : ConsumableBase
    {
    }
}
