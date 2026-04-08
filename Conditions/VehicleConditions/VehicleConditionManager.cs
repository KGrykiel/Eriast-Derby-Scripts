using Assets.Scripts.Combat;
using Assets.Scripts.Combat.Damage;
using Assets.Scripts.Combat.Restoration;
using Assets.Scripts.Entities;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Entities.Vehicles.VehicleComponents;
using Assets.Scripts.Modifiers;
using UnityEngine;

namespace Assets.Scripts.Conditions.VehicleConditions
{
    /// <summary>
    /// Per-vehicle manager for vehicle-wide conditions.
    /// OnActivate routes each attribute modifier to its owning component via VehicleComponentResolver.
    /// OnDeactivate removes all modifiers and advantage grants from all components as a unit.
    /// OnTick fires periodic effects against every component on the vehicle.
    /// </summary>
    public class VehicleConditionManager : ConditionManagerBase<VehicleCondition, AppliedVehicleCondition>
    {
        private readonly Vehicle vehicle;

        public VehicleConditionManager(Vehicle vehicle)
        {
            this.vehicle = vehicle;
        }

        // ==================== ABSTRACT OVERRIDES ====================

        protected override bool CanApply(VehicleCondition template) => true;

        protected override AppliedVehicleCondition CreateApplied(VehicleCondition template, Object applier)
            => new(template, applier);

        protected override float GetTemplateMagnitude(VehicleCondition template)
        {
            float total = 0f;
            foreach (var m in template.modifiers)
                total += Mathf.Abs(m.value);
            return total;
        }

        protected override string GetOwnerDisplayName()
            => vehicle != null ? vehicle.vehicleName : "Unknown Vehicle";

        // ==================== LIFECYCLE HOOKS ====================

        protected override void OnActivate(AppliedVehicleCondition applied)
        {
            foreach (var modData in applied.template.modifiers)
            {
                VehicleComponent component = VehicleComponentResolver.ResolveForAttribute(vehicle, modData.attribute);
                if (component == null)
                    component = vehicle.Chassis;
                if (component != null)
                {
                    var modifier = new EntityAttributeModifier(modData.attribute, modData.type, modData.value) { Source = applied };
                    component.AddModifier(modifier);
                }
            }
        }

        protected override void OnDeactivate(AppliedVehicleCondition applied)
        {
            foreach (var component in vehicle.AllComponents)
            {
                component.RemoveModifiersFromSource(applied);
                component.RemoveAdvantageGrantsFromSource(applied);
            }
        }

        protected override void OnTick(AppliedVehicleCondition applied)
        {
            foreach (var periodic in applied.template.periodicEffects)
            {
                foreach (var component in vehicle.AllComponents)
                {
                    switch (periodic)
                    {
                        case PeriodicDamageEffect dot:
                            var resistance = component.GetResistance(dot.damageFormula.damageType);
                            var dmgResult = DamageCalculator.Compute(dot.damageFormula, resistance);
                            DamageApplicator.Apply(dmgResult, component, causalSource: applied.template.effectName);
                            break;

                        case PeriodicRestorationEffect hot:
                            int amount = RestorationCalculator.Compute(hot.formula);
                            RestorationApplicator.Apply(hot.formula, amount, component, causalSource: applied.template.effectName);
                            break;
                    }
                }
            }
        }

        // ==================== EVENT HOOKS ====================

        protected override void OnNewlyApplied(AppliedVehicleCondition applied, bool wasReplacement)
        {
            Entity sourceEntity = applied.applier as Entity;
            string applierName = applied.applier != null ? applied.applier.name : null;
            CombatEventBus.Emit(new VehicleConditionEvent(applied, sourceEntity, vehicle, applierName));
        }

        protected override void OnExpired(AppliedVehicleCondition applied)
            => CombatEventBus.Emit(new VehicleConditionExpiredEvent(applied, vehicle));

        protected override void OnRefreshed(AppliedVehicleCondition applied)
            => CombatEventBus.Emit(new VehicleConditionRefreshedEvent(applied, vehicle));

        protected override void OnIgnored(AppliedVehicleCondition applied)
            => CombatEventBus.Emit(new VehicleConditionIgnoredEvent(applied, vehicle));

        protected override void OnStackLimitReached(VehicleCondition template)
            => CombatEventBus.Emit(new VehicleConditionStackLimitEvent(template, vehicle, template.maxStacks));

        protected override void OnReplaced(AppliedVehicleCondition newApplied, int oldDuration)
            => CombatEventBus.Emit(new VehicleConditionReplacedEvent(newApplied, vehicle, oldDuration));

        protected override void OnKeptStronger(AppliedVehicleCondition applied)
            => CombatEventBus.Emit(new VehicleConditionKeptStrongerEvent(applied, vehicle));

        protected override void OnRemovedByTrigger(AppliedVehicleCondition applied, RemovalTrigger trigger)
            => CombatEventBus.Emit(new VehicleConditionRemovedByTriggerEvent(applied, vehicle, trigger));
    }
}
