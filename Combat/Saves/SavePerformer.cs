using UnityEngine;

namespace Assets.Scripts.Combat.Saves
{
    /// <summary>
    /// Universal entry point for saving throws.
    /// Handles routing + computation + event emission in one call.
    /// Inspired by WOTR's Rulebook system - execution automatically logs events.
    /// 
    /// Used by: Skills, Events, Stages, Status Effects, and any system that needs saves.
    /// </summary>
    public static class SavePerformer
    {
        /// <summary>
        /// Execute a saving throw for a vehicle.
        /// Routes internally, computes, emits events automatically.
        /// </summary>
        /// <param name="vehicle">Vehicle making the save</param>
        /// <param name="spec">What type of save</param>
        /// <param name="dc">Difficulty class</param>
        /// <param name="causalSource">What triggered this save (Skill, Card, Stage, etc.)</param>
        /// <param name="targetComponent">Targeted component (for attack-based saves)</param>
        /// <param name="attackerEntity">Attacker entity (null for non-attack saves like hazards)</param>
        public static SaveResult Execute(
            Vehicle vehicle,
            SaveSpec spec,
            int dc,
            Object causalSource,
            VehicleComponent targetComponent = null,
            Entity attackerEntity = null)
        {
            // Step 1: Route (resolve who/what makes this save)
            var routing = CheckRouter.RouteSave(vehicle, spec, targetComponent);

            // Step 2: Compute
            SaveResult result;
            if (!routing.CanAttempt)
            {
                result = SaveCalculator.AutoFail(spec, dc);
            }
            else
            {
                result = SaveCalculator.Compute(spec, dc, routing.Component, routing.Character);
            }

            // Step 3: Emit event automatically (WOTR-style)
            Entity defenderEntity = routing.Component ?? vehicle.chassis;
            string targetName = targetComponent != null ? targetComponent.name : "Vehicle";
            CombatEventBus.EmitSavingThrow(
                result,
                attackerEntity,
                defenderEntity,
                causalSource,
                result.Roll.Success,
                targetName,
                result.Character);

            return result;
        }

        /// <summary>
        /// Execute a saving throw for a standalone entity (no vehicle routing).
        /// Used for NPCs, props, or any non-vehicle entity making a save.
        /// </summary>
        public static SaveResult ExecuteForEntity(
            Entity entity,
            SaveSpec spec,
            int dc,
            Object causalSource,
            Character character = null,
            Entity attackerEntity = null)
        {
            var result = SaveCalculator.Compute(spec, dc, entity, character);

            string targetName = entity != null ? entity.name : "Target";
            CombatEventBus.EmitSavingThrow(
                result,
                attackerEntity,
                entity,
                causalSource,
                result.Roll.Success,
                targetName,
                character);

            return result;
        }
    }
}
