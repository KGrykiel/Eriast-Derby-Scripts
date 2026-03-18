using UnityEngine;
using Assets.Scripts.Entities.Vehicle.VehicleComponents.ComponentTypes;

namespace Assets.Scripts.Combat.Restoration
{
    /// <summary>
    /// Single entry point for ALL restoration/drain application.
    /// Applies target changes and emits restoration events consistently.
    /// </summary>
    public static class RestorationApplicator
    {
        public static RestorationResult Apply(
            RestorationFormula formula,
            int rolledAmount,
            Entity target,
            Entity source = null,
            Object causalSource = null)
        {
            var result = formula.resourceType switch
            {
                ResourceType.Health => ApplyHealth(formula, rolledAmount, target),
                ResourceType.Energy => ApplyEnergy(formula, rolledAmount, target),
                _ => BuildNoChangeResult(formula, rolledAmount)
            };

            CombatEventBus.EmitRestoration(result, source, target, causalSource);
            return result;
        }

        private static RestorationResult ApplyHealth(RestorationFormula formula, int amount, Entity target)
        {
            int oldValue = target.GetCurrentHealth();
            int maxValue = target.GetMaxHealth();

            if (amount >= 0)
                target.Heal(amount);

            int newValue = target.GetCurrentHealth();
            int actualChange = newValue - oldValue;

            return new RestorationResult(
                resourceType: formula.resourceType,
                diceCount: formula.baseDice,
                dieSize: formula.dieSize,
                bonus: formula.bonus,
                rawTotal: amount,
                oldValue: oldValue,
                newValue: newValue,
                maxValue: maxValue,
                requestedChange: amount,
                actualChange: actualChange);
        }

        private static RestorationResult ApplyEnergy(RestorationFormula formula, int amount, Entity target)
        {
            if (target is PowerCoreComponent powerCore)
            {
                int oldValue = powerCore.currentEnergy;
                int maxValue = powerCore.GetMaxEnergy();

                int actualChange = amount >= 0 
                    ? powerCore.RestoreEnergy(amount) 
                    : -powerCore.DrainEnergy(-amount);

                int newValue = powerCore.currentEnergy;

                return new RestorationResult(
                    resourceType: formula.resourceType,
                    diceCount: formula.baseDice,
                    dieSize: formula.dieSize,
                    bonus: formula.bonus,
                    rawTotal: amount,
                    oldValue: oldValue,
                    newValue: newValue,
                    maxValue: maxValue,
                    requestedChange: amount,
                    actualChange: actualChange);
            }

            Debug.LogWarning($"[RestorationApplicator] Energy restoration requires PowerCoreComponent target. Got: {target.GetType().Name}");
            return BuildNoChangeResult(formula, amount);
        }

        private static RestorationResult BuildNoChangeResult(RestorationFormula formula, int amount)
        {
            return new RestorationResult(
                resourceType: formula.resourceType,
                diceCount: formula.baseDice,
                dieSize: formula.dieSize,
                bonus: formula.bonus,
                rawTotal: amount,
                oldValue: 0,
                newValue: 0,
                maxValue: 0,
                requestedChange: amount,
                actualChange: 0);
        }
    }
}
