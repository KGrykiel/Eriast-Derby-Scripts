using System.Collections.Generic;
using Assets.Scripts.Effects;
using UnityEngine;
using Assets.Scripts.Combat.Rolls.RollTypes.Attacks;
using Assets.Scripts.Combat.Rolls.RollTypes.OpposedChecks;
using Assets.Scripts.Combat.Rolls.RollTypes.Saves;
using Assets.Scripts.Combat.Rolls.RollTypes.SkillChecks;
using Assets.Scripts.Combat.Rolls.RollSpecs.SpecTypes;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Entities;
using Assets.Scripts.Entities.Vehicles.VehicleComponents;
using Assets.Scripts.Effects.Invocations;

namespace Assets.Scripts.Combat.Rolls.RollSpecs
{
    /// <summary>
    /// Unified executor for RollNode resolution.
    /// Used by skills, event cards, and lane effects.
    /// Callers fill only the RollContext fields they have.
    /// Source vehicle is derived: ctx.SourceActor?.GetVehicle() ?? ctx.Target as Vehicle.
    /// </summary>
    public static class RollNodeExecutor
    {
        /// <summary>
        /// Executes a RollNode: makes the configured roll, applies the appropriate effects,
        /// then follows the success or failure chain if one is set.
        /// </summary>
        /// <param name="node">Node to execute. Null is treated as unconditional success with no effects.</param>
        /// <param name="ctx">Execution context. Source vehicle is derived from context, not passed explicitly.</param>
        /// <returns>True if the roll succeeded (or there was no roll), false on failure.</returns>
        public static bool Execute(RollNode node, RollContext ctx)
        {
            if (node == null)
                return true;
            return ExecuteNode(node, ctx);
        }

        private static bool ExecuteNode(RollNode node, RollContext ctx)
        {
            // Fan-out: execute once per resolved target
            if (node.targetResolver != null)
            {
                var targets = node.targetResolver.ResolveFrom(ctx);
                bool anySuccess = false;
                foreach (var target in targets)
                {
                    var perTargetCtx = new RollContext
                    {
                        SourceActor = ctx.SourceActor,
                        Target = target,
                        CausalSource = ctx.CausalSource
                    };
                    bool result = ExecuteSingleNode(node, perTargetCtx);
                    anySuccess |= result;
                }
                return anySuccess;
            }

            throw new System.InvalidOperationException( $"[RollNodeExecutor] RollNode has no targetResolver. This is a content configuration error. CausalSource: {ctx.CausalSource ?? "<none>"}");
        }

        private static bool ExecuteSingleNode(RollNode node, RollContext ctx)
        {
            List<RollNode> chains;
            bool success;

            CombatEventBus.BeginAction();
            try
            {
                D20RollOutcome roll = ResolveRoll(node.rollSpec, ctx);

                var effects = roll.Success ? node.successEffects : node.failureEffects;
                ApplyEffects(effects, ctx, roll.IsCriticalHit);

                chains = roll.Success ? node.onSuccessChains : node.onFailureChains;
                success = roll.Success;
            }
            finally
            {
                CombatEventBus.EndAction();
            }

            foreach (var chain in chains)
                ExecuteNode(chain, ctx);

            return success;
        }

        // ==================== ROLL RESOLUTION ====================

        private static D20RollOutcome ResolveRoll(IRollSpec spec, RollContext ctx)
        {
            return spec switch
            {
                null                     => D20RollOutcome.AutoSuccess(0),
                SkillCheckSpec s         => ResolveSkillCheck(s, ctx),
                SaveSpec s               => ResolveSavingThrow(s, ctx),
                AttackSpec s             => ResolveAttack(s, ctx),
                StateThresholdSpec s     => ResolveStateThreshold(s, ctx),
                OpposedCheckRollSpec s   => ResolveOpposedCheck(s, ctx),
                _                        => D20RollOutcome.AutoFail(0)
            };
        }

        private static D20RollOutcome ResolveSkillCheck(SkillCheckSpec spec, RollContext ctx)
        {
            bool useTarget = spec.roller == RollerSource.Target;
            Vehicle roller = useTarget ? GetTargetVehicle(ctx) : GetSourceVehicle(ctx);
            RollActor actorHint = useTarget ? GetTargetActor(ctx) : ctx.SourceActor;
            var routing = CheckRouter.RouteSkillCheck(roller, spec, actorHint);
            var checkCtx = new SkillCheckExecutionContext
            {
                Vehicle = roller,
                Spec = spec,
                CausalSource = ctx.CausalSource,
                Routing = routing
            };

            return SkillCheckPerformer.Execute(checkCtx);
        }

        private static D20RollOutcome ResolveSavingThrow(SaveSpec spec, RollContext ctx)
        {
            Vehicle roller = spec.roller == RollerSource.Source
                ? GetSourceVehicle(ctx)
                : GetTargetVehicle(ctx) ?? GetSourceVehicle(ctx);

            var routing = CheckRouter.RouteSave(roller, spec, ctx.Target as VehicleComponent);

            var saveCtx = new SaveExecutionContext
            {
                Vehicle = roller,
                Spec = spec,
                CausalSource = ctx.CausalSource,
                AttackerEntity = ctx.SourceActor?.GetEntity(),
                Routing = routing
            };

            return SavePerformer.Execute(saveCtx);
        }

        private static D20RollOutcome ResolveAttack(AttackSpec spec, RollContext ctx)
        {
            Entity targetEntity = ResolveTargetEntity(ctx);
            if (targetEntity == null)
            {
                Debug.LogError("[RollNodeExecutor] AttackSpec requires a targetable Entity in RollContext.");
                return D20RollOutcome.AutoFail(0);
            }

            var attackCtx = new AttackExecutionContext
            {
                Spec         = spec,
                Target       = targetEntity,
                CausalSource = ctx.CausalSource,
                Attacker     = ctx.SourceActor
            };

            return AttackPerformer.Execute(attackCtx);
        }

        private static D20RollOutcome ResolveStateThreshold(StateThresholdSpec spec, RollContext ctx)
        {
            Vehicle sourceVehicle = GetSourceVehicle(ctx);
            if (sourceVehicle == null)
            {
                Debug.LogError("[RollNodeExecutor] StateThresholdSpec requires a source vehicle in RollContext.");
                return D20RollOutcome.AutoFail(0);
            }

            int value = sourceVehicle.GetStateValue(spec.state);
            bool success = value >= spec.minimumValue;

            CombatEventBus.Emit(new StateThresholdEvent(sourceVehicle, spec.state, value, spec.minimumValue, success, ctx.CausalSource));

            return success ? D20RollOutcome.AutoSuccess(0) : D20RollOutcome.AutoFail(0);
        }

        private static D20RollOutcome ResolveOpposedCheck(OpposedCheckRollSpec spec, RollContext ctx)
        {
            Vehicle targetVehicle = GetTargetVehicle(ctx);
            if (targetVehicle == null)
            {
                Debug.LogError("[RollNodeExecutor] OpposedCheck requires a target vehicle in RollContext.");
                return D20RollOutcome.AutoFail(0);
            }

            Vehicle sourceVehicle = GetSourceVehicle(ctx);
            var attackerRouting = CheckRouter.RouteSkillCheck(sourceVehicle, spec.attackerSpec, ctx.SourceActor);
            var defenderRouting = CheckRouter.RouteSkillCheck(targetVehicle, spec.defenderSpec);

            var checkCtx = new OpposedCheckExecutionContext
            {
                AttackerVehicle = sourceVehicle,
                DefenderVehicle = targetVehicle,
                Spec = spec,
                CausalSource = ctx.CausalSource,
                AttackerRouting = attackerRouting,
                DefenderRouting = defenderRouting
            };

            return OpposedCheckPerformer.Execute(checkCtx);
        }

        // ==================== EFFECT APPLICATION ====================

        private static void ApplyEffects(List<IEffectInvocation> effects, RollContext ctx, bool isCriticalHit)
        {
            if (effects == null || effects.Count == 0) return;

            var effectContext = EffectContext.FromRollContext(ctx, isCriticalHit);
            effectContext.CausalSource = ctx.CausalSource;

            foreach (var invocation in effects)
                invocation?.Execute(ctx, effectContext);
        }

        // ==================== CONTEXT HELPERS ====================

        private static Vehicle GetSourceVehicle(RollContext ctx)
        {
            if (ctx.SourceActor != null)
            {
                Vehicle vehicle = ctx.SourceActor.GetVehicle();
                if (vehicle != null) return vehicle;
            }
            return ctx.Target as Vehicle;
        }

        private static Vehicle GetTargetVehicle(RollContext ctx)
        {
            return ctx.Target switch
            {
                Vehicle vehicle => vehicle,
                Entity entity => EntityHelpers.GetParentVehicle(entity),
                VehicleSeat seat => seat.ParentVehicle,
                _ => null
            };
        }

        private static RollActor GetTargetActor(RollContext ctx)
        {
            return ctx.Target switch
            {
                VehicleSeat seat => new CharacterActor(seat),
                Entity entity    => new ComponentActor(entity),
                _                => null
            };
        }

        private static Entity ResolveTargetEntity(RollContext ctx)
        {
            return ctx.Target switch
            {
                Entity entity => entity,
                Vehicle vehicle => vehicle.Chassis,
                _ => null
            };
        }
    }
}
