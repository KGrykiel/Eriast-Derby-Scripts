using System.Collections.Generic;
using Assets.Scripts.Combat;
using Assets.Scripts.Effects;
using Assets.Scripts.Conditions;
using UnityEngine;
using Assets.Scripts.Combat.Rolls.RollTypes.Attacks;
using Assets.Scripts.Combat.Rolls.RollTypes.OpposedChecks;
using Assets.Scripts.Combat.Rolls.RollTypes.Saves;
using Assets.Scripts.Combat.Rolls.RollTypes.SkillChecks;
using Assets.Scripts.Combat.Rolls.RollSpecs.SpecTypes;

namespace Assets.Scripts.Combat.Rolls.RollSpecs
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
        public static bool Execute(RollNode node, RollContext ctx, string causalSource = null, bool scopeAsAction = false)
        {
            if (node == null)
                return true;

            return ExecuteNode(node, ctx, causalSource, scopeAsAction);
        }

        private static bool ExecuteNode(RollNode node, RollContext ctx, string causalSource, bool scopeAsAction)
        {
            D20RollOutcome roll = ResolveRoll(node.rollSpec, ctx, causalSource);

            var effects = roll.Success ? node.successEffects : node.failureEffects;
            ApplyEffects(effects, ctx, roll.IsCriticalHit, causalSource, scopeAsAction);

            var chain = roll.Success ? node.onSuccessChain : node.onFailureChain;
            if (chain != null)
                ExecuteNode(chain, ctx, causalSource, scopeAsAction);

            return roll.Success;
        }

        // ==================== ROLL RESOLUTION ====================

        private static D20RollOutcome ResolveRoll(IRollSpec spec, RollContext ctx, string causalSource)
        {
            return spec switch
            {
                null                     => D20Calculator.AutoSuccess(0),
                SkillCheckSpec s         => ResolveSkillCheck(s, ctx, causalSource),
                SaveSpec s               => ResolveSavingThrow(s, ctx, causalSource),
                AttackSpec s             => ResolveAttack(s, ctx, causalSource),
                StateThresholdSpec s     => ResolveStateThreshold(s, ctx),
                OpposedCheckRollSpec s   => ResolveOpposedCheck(s, ctx, causalSource),
                _                        => D20Calculator.AutoFail(0)
            };
        }

        private static D20RollOutcome ResolveSkillCheck(SkillCheckSpec spec, RollContext ctx, string causalSource)
        {
            var routing = CheckRouter.RouteSkillCheck(ctx.SourceVehicle, spec, ctx.SourceActor);
            var checkCtx = new SkillCheckExecutionContext
            {
                Vehicle = ctx.SourceVehicle,
                Spec = spec,
                CausalSource = causalSource,
                Routing = routing
            };

            return SkillCheckPerformer.Execute(checkCtx);
        }

        private static D20RollOutcome ResolveSavingThrow(SaveSpec spec, RollContext ctx, string causalSource)
        {
            // In skill context the target makes the save; in event/lane context the acting vehicle makes it.
            Vehicle targetVehicle = EntityHelpers.GetParentVehicle(ctx.TargetEntity);
            Vehicle roller = targetVehicle != null ? targetVehicle : ctx.SourceVehicle;

            var routing = CheckRouter.RouteSave(roller, spec, ctx.TargetEntity as VehicleComponent);

            var saveCtx = new SaveExecutionContext
            {
                Vehicle = roller,
                Spec = spec,
                CausalSource = causalSource,
                AttackerEntity = ctx.SourceActor?.GetEntity(),
                Routing = routing
            };

            return SavePerformer.Execute(saveCtx);
        }

        private static D20RollOutcome ResolveAttack(AttackSpec spec, RollContext ctx, string causalSource)
        {
            if (ctx.TargetEntity == null)
            {
                Debug.LogError("[RollNodeExecutor] AttackSpec requires a TargetEntity in RollContext.");
                return D20Calculator.AutoFail(0);
            }

            var attackCtx = new AttackExecutionContext
            {
                Spec         = spec,
                Target       = ctx.TargetEntity,
                CausalSource = causalSource,
                Attacker     = ctx.SourceActor
            };

            return AttackPerformer.Execute(attackCtx);
        }

        private static D20RollOutcome ResolveStateThreshold(StateThresholdSpec spec, RollContext ctx)
        {
            int value = ctx.SourceVehicle.GetStateValue(spec.state);
            bool success = value >= spec.minimumValue;

            Debug.Log($"[StateThreshold] {ctx.SourceVehicle?.vehicleName}: {spec.state} {value} vs minimum {spec.minimumValue} — {(success ? "PASS" : "FAIL")}");

            return success ? D20Calculator.AutoSuccess(0) : D20Calculator.AutoFail(0);
        }

        private static D20RollOutcome ResolveOpposedCheck(OpposedCheckRollSpec spec, RollContext ctx, string causalSource)
        {
            Vehicle targetVehicle = EntityHelpers.GetParentVehicle(ctx.TargetEntity);
            if (targetVehicle == null)
            {
                Debug.LogError("[RollNodeExecutor] OpposedCheck requires a TargetVehicle in RollContext.");
                return D20Calculator.AutoFail(0);
            }

            var attackerRouting = CheckRouter.RouteSkillCheck(ctx.SourceVehicle, spec.attackerSpec, ctx.SourceActor);
            var defenderRouting = CheckRouter.RouteSkillCheck(targetVehicle, spec.defenderSpec);

            var checkCtx = new OpposedCheckExecutionContext
            {
                AttackerVehicle = ctx.SourceVehicle,
                DefenderVehicle = targetVehicle,
                Spec = spec,
                CausalSource = causalSource,
                AttackerRouting = attackerRouting,
                DefenderRouting = defenderRouting
            };

            return OpposedCheckPerformer.Execute(checkCtx);
        }

        // ==================== EFFECT APPLICATION ====================

        private static void ApplyEffects(List<EffectInvocation> effects, RollContext ctx, bool isCriticalHit, string causalSource, bool scopeAsAction)
        {
            if (effects == null || effects.Count == 0) return;

            if (scopeAsAction)
            {
                Vehicle targetVehicle = EntityHelpers.GetParentVehicle(ctx.TargetEntity);
                CombatEventBus.BeginAction(ctx.SourceActor, causalSource, targetVehicle, ctx.SourceVehicle);
                VehicleComponent sourceComponent = ctx.SourceActor?.GetEntity() as VehicleComponent;
                if (sourceComponent != null)
                    sourceComponent.NotifyStatusEffectTrigger(RemovalTrigger.OnSkillUsed);
            }

            try
            {
                var effectContext = EffectContext.FromRollContext(ctx, isCriticalHit);

                foreach (var invocation in effects)
                {
                    if (invocation?.effect == null) continue;

                    var targets = ResolveTargets(invocation, ctx);
                    foreach (var target in targets)
                        invocation.effect.Apply(target, effectContext);
                }
            }
            finally
            {
                if (scopeAsAction)
                    CombatEventBus.EndAction();
            }
        }

        private static List<Entity> ResolveTargets(EffectInvocation invocation, RollContext ctx)
        {
            var targets = new List<Entity>();

            switch (invocation.target)
            {
                case EffectTarget.SourceComponent:
                    VehicleComponent sourceComponent = ctx.SourceActor?.GetEntity() as VehicleComponent;
                    if (sourceComponent != null)
                        targets.Add(sourceComponent);
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
                    Vehicle targetVehicle = EntityHelpers.GetParentVehicle(ctx.TargetEntity);
                    Vehicle routeVehicle = targetVehicle != null ? targetVehicle : ctx.SourceVehicle;
                    targets.Add(routeVehicle.RouteEffectTarget(invocation.effect));
                    break;
            }

            return targets;
        }
    }
}
