using Assets.Scripts.Modifiers;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Core
{
    /// <summary>
    /// Shared calculation logic for the modifier system.
    /// Used by both StatCalculator (entity) and CharacterStatCalculator (character).
    /// </summary>
    public static class ModifierCalculator
    {
        /// <summary>
        /// Calculate total value from base + modifiers.
        /// Application order (D&D standard): base → all Flat → all Multiplier → round once.
        /// </summary>
        public static int CalculateTotal(int baseValue, IEnumerable<ModifierBase> modifiers)
        {
            float total = baseValue;

            foreach (var mod in modifiers)
                if (mod.Type == ModifierType.Flat)
                    total += mod.Value;

            foreach (var mod in modifiers)
                if (mod.Type == ModifierType.Multiplier)
                    total *= mod.Value;

            return Mathf.RoundToInt(total);
        }
    }
}
