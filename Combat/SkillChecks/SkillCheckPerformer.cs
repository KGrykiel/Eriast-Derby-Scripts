using UnityEngine;

namespace Assets.Scripts.Combat.SkillChecks
{
    /// <summary>
    /// Universal entry point for skill checks.
    /// Handles routing + computation + event emission in one call.
    /// Inspired by WOTR's Rulebook system - execution automatically logs events.
    /// 
    /// Used by: Skills, Events, Stages, Status Effects, and any system that needs skill checks.
    /// </summary>
    public static class SkillCheckPerformer
    {
        /// <summary>
        /// Execute a skill check for a vehicle.
        /// Routes internally, computes, emits events automatically.
        /// </summary>
        /// <param name="vehicle">Vehicle making the check</param>
        /// <param name="spec">What type of check</param>
        /// <param name="dc">Difficulty class</param>
        /// <param name="causalSource">What triggered this check (Skill, Card, Stage, etc.)</param>
        /// <param name="initiatingCharacter">Character who initiated the check (null for vehicle-wide checks)</param>
        public static SkillCheckResult Execute(
            Vehicle vehicle,
            SkillCheckSpec spec,
            int dc,
            Object causalSource,
            Character initiatingCharacter = null)
        {
            // Step 1: Route (resolve who/what makes this check)
            var routing = CheckRouter.RouteSkillCheck(vehicle, spec, initiatingCharacter);

            // Step 2: Compute
            SkillCheckResult result;
            if (!routing.CanAttempt)
            {
                result = SkillCheckCalculator.AutoFail(spec, dc);
            }
            else
            {
                result = SkillCheckCalculator.Compute(spec, dc, routing.Component, routing.Character);
            }

            // Step 3: Emit event automatically (WOTR-style)
            Entity sourceEntity = routing.Component ?? vehicle.chassis;
            CombatEventBus.EmitSkillCheck(
                result,
                sourceEntity,
                causalSource,
                result.Roll.Success,
                result.Character);

            return result;
        }

        /// <summary>
        /// Execute a skill check for a standalone entity (no vehicle routing).
        /// Used for NPCs, props, or any non-vehicle entity making a check.
        /// </summary>
        public static SkillCheckResult ExecuteForEntity(
            Entity entity,
            SkillCheckSpec spec,
            int dc,
            Object causalSource,
            Character character = null)
        {
            var result = SkillCheckCalculator.Compute(spec, dc, entity, character);

            CombatEventBus.EmitSkillCheck(
                result,
                entity,
                causalSource,
                result.Roll.Success,
                character);

            return result;
        }
    }
}
