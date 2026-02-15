using UnityEngine;

namespace Assets.Scripts.Combat.SkillChecks
{
    /// <summary>
    /// Entry point for skill checks from all sources — skills, event cards, maybe more in the future.
    /// different methods for vehicles that need routing vs standalone entities that don't (objects, monsters etc).
    /// </summary>
    public static class SkillCheckPerformer
    {
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

        /// <summary>Standalone entity overload — no vehicle routing.</summary>
        public static SkillCheckResult ExecuteForEntity(
            Entity entity,
            SkillCheckSpec spec,
            int dc,
            Object causalSource)
        {
            var result = SkillCheckCalculator.Compute(spec, dc, entity);

            CombatEventBus.EmitSkillCheck(
                result,
                entity,
                causalSource,
                result.Roll.Success);

            return result;
        }
    }
}
