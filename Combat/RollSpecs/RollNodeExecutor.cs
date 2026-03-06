using System.Collections.Generic;
using Assets.Scripts.Combat.Attacks;
using Assets.Scripts.Combat.SkillChecks;
using Assets.Scripts.Combat.Saves;
using Assets.Scripts.Effects;
using UnityEngine;

namespace Assets.Scripts.Combat.RollSpecs
{
    /// <summary>
    /// Unified executor for RollNode resolution.
    /// Used by skills, event cards, and lane effects.
    /// Callers fill only the SkillContext fields they have — SourceVehicle is the only required field.
    /// </summary>
    public static class RollNodeExecutor
    {
        /// <summary>
        /// Executes a RollNode: makes the configured roll, applies the appropriate effects,
        /// then follows the success or failure chain if one is set.
        /// </summary>
        /// <param name="node">Node to execute. Null is treated as unconditional success with no effects.</param>
        /// <param name="ctx">Execution context. Only SourceVehicle is required for event/lane callers.</param>
        /// <param name="causalSource">What triggered this node, used for logging.</param>
        /// <returns>True if the roll succeeded (or there was no roll), false on failure.</returns>
        public static bool Execute(RollNode node, RollContext ctx, Object causalSource = null)
        {
            if (node == null)
                return true;

            var (success, effectCtx) = ResolveRoll(node, ctx, causalSource);

            var effects = success ? node.successEffects : node.failureEffects;
            ApplyEffects(effects, effectCtx, causalSource);

            var chain = success ? node.onSuccessChain : node.onFailureChain;
            if (chain != null)
                Execute(chain, effectCtx, causalSource);

            return success;
        }

        // ==================== ROLL RESOLUTION ====================

        private static (bool success, RollContext updatedCtx) ResolveRoll(RollNode node, RollContext ctx, Object causalSource)
        {
            return node.rollSpec switch
            {
                null                 => (true, ctx),
                SkillCheckSpec spec  => (ResolveSkillCheck(spec, node.dc, ctx, causalSource), ctx),
                SaveSpec spec        => (ResolveSavingThrow(spec, node.dc, ctx, causalSource), ctx),
                AttackSpec spec      => ResolveAttack(spec, node, ctx, causalSource),
                OpposedCheckRollSpec => (ResolveOpposedCheck(ctx), ctx),
                _                    => (false, ctx)
            };
        }

        private static bool ResolveSkillCheck(SkillCheckSpec spec, int dc, RollContext ctx, Object causalSource)
        {
            var checkCtx = new SkillCheckExecutionContext
            {
                Vehicle = ctx.SourceVehicle,
                Spec = spec,
                DC = dc,
                CausalSource = causalSource,
                InitiatingCharacter = ctx.SourceCharacter
            };

            var result = SkillCheckPerformer.Execute(checkCtx);
            return result.Roll.Success;
        }

        private static bool ResolveSavingThrow(SaveSpec spec, int dc, RollContext ctx, Object causalSource)
        {
            // In skill context the target makes the save; in event/lane context the acting vehicle makes it.
            Vehicle roller = ctx.TargetVehicle != null ? ctx.TargetVehicle : ctx.SourceVehicle;

            var saveCtx = new SaveExecutionContext
            {
                Vehicle = roller,
                Spec = spec,
                DC = dc,
                CausalSource = causalSource,
                TargetComponent = ctx.TargetComponent,
                AttackerEntity = ctx.SourceComponent
            };

            var result = SavePerformer.Execute(saveCtx);
            return result.Roll.Success;
        }

        private static (bool success, RollContext updatedCtx) ResolveAttack(AttackSpec spec, RollNode node, RollContext ctx, Object causalSource)
        {
            if (ctx.TargetEntity == null)
            {
                Debug.LogError("[RollNodeExecutor] AttackSpec requires a TargetEntity in RollContext.");
                return (false, ctx);
            }

            var attackCtx = new AttackExecutionContext
            {
                Spec         = spec,
                Target       = ctx.TargetEntity,
                CausalSource = causalSource,
                Attacker     = ctx.SourceComponent,
                Character    = ctx.SourceCharacter
            };

            var result = AttackPerformer.Execute(attackCtx);

            // Propagate retargeting (component fallback) and crit status for downstream effects.
            Entity finalTarget = result.HitTarget != null ? result.HitTarget : ctx.TargetEntity;
            var updatedCtx = ctx
                .WithTarget(finalTarget)
                .WithCriticalHit(result.Roll.IsCriticalHit);

            return (result.HitTarget != null, updatedCtx);
        }

        private static bool ResolveOpposedCheck(RollContext ctx)
        {
            Debug.LogWarning("[RollNodeExecutor] OpposedCheck is not yet implemented.");
            return false;
        }

        // ==================== EFFECT APPLICATION ====================

        private static void ApplyEffects(List<EffectInvocation> effects, RollContext ctx, Object causalSource)
        {
            if (effects == null || effects.Count == 0) return;

            // Wrap in a CombatEventBus action only when executing a skill (aggregates multi-effect logging).
            bool useEventBus = causalSource is Skill;
            if (useEventBus)
                CombatEventBus.BeginAction(ctx.SourceEntity, causalSource as Skill, ctx.TargetVehicle, ctx.SourceVehicle, ctx.SourceCharacter);

            try
            {
                var effectContext = EffectContext.FromRollContext(ctx);
                Object source = causalSource;

                foreach (var invocation in effects)
                {
                    if (invocation?.effect == null) continue;

                    var targets = ResolveTargets(invocation, ctx);
                    foreach (var target in targets)
                        invocation.effect.Apply(target, effectContext, source);
                }
            }
            finally
            {
                if (useEventBus)
                    CombatEventBus.EndAction();
            }
        }

        private static List<Entity> ResolveTargets(EffectInvocation invocation, RollContext ctx)
        {
            var targets = new List<Entity>();

            switch (invocation.target)
            {
                case EffectTarget.SourceComponent:
                    if (ctx.SourceComponent != null)
                        targets.Add(ctx.SourceComponent);
                    else
                        Debug.LogWarning("[RollNodeExecutor] EffectTarget.SourceComponent with no SourceComponent in context.");
                    break;

                case EffectTarget.SourceVehicle:
                    targets.Add(ctx.SourceVehicle.RouteEffectTarget(invocation.effect));
                    break;

                case EffectTarget.SourceComponentSelection:
                case EffectTarget.SelectedTarget:
                    if (ctx.TargetEntity != null)
                        targets.Add(ctx.TargetEntity);
                    break;

                case EffectTarget.TargetVehicle:
                    Vehicle routeVehicle = ctx.TargetVehicle != null ? ctx.TargetVehicle : ctx.SourceVehicle;
                    targets.Add(routeVehicle.RouteEffectTarget(invocation.effect));
                    break;
            }

            return targets;
        }
    }
}
