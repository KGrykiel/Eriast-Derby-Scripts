using Assets.Scripts.Combat.Damage;
using Assets.Scripts.Combat.Damage.FormulaProviders.SpecificProviders;
using Assets.Scripts.Combat.Restoration;
using Assets.Scripts.Combat.Rolls.RollSpecs;
using Assets.Scripts.Effects;
using Assets.Scripts.Effects.EffectTypes.EntityEffects;
using Assets.Scripts.Effects.EffectTypes.VehicleEffects;
using Assets.Scripts.Effects.Invocations;
using Assets.Scripts.Entities;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Managers.Turn;
using Assets.Scripts.Skills;
using UnityEngine;

namespace Assets.Scripts.AI.Scoring
{
    /// <summary>
    /// Pure scoring helper. Inspects the concrete typed effects on a skill's
    /// roll node and returns a single utility float via dot product against the
    /// command weight vector.
    /// </summary>
    public static class SkillScorer
    {
        // Normalisation factors: "saturation points" above which each axis adds no further utility.
        private const float DamageSaturation = 20f;
        private const float RestorationSaturation = 20f;
        private const float ProgressSaturation = 20f;

        public static float Score(Skill skill, CommandWeights weights, VehicleAISharedContext context, IRollTarget target)
        {
            if (skill == null || skill.rollNode == null) return 0f;

            bool targetIsEnemy = IsEnemy(context, target);
            CommandWeights utility = default;

            AccumulateNode(skill.rollNode, context, target, targetIsEnemy, ref utility);

            return weights.attack  * utility.attack
                 + weights.heal    * utility.heal
                 + weights.disrupt * utility.disrupt
                 + weights.flee    * utility.flee;
        }

        // ==================== NODE WALK ====================

        private static void AccumulateNode(RollNode node, VehicleAISharedContext context, IRollTarget target, bool targetIsEnemy, ref CommandWeights utility)
        {
            if (node == null) return;

            if (node.successEffects != null)
            {
                foreach (var inv in node.successEffects)
                    AccumulateInvocation(inv, context, targetIsEnemy, ref utility);
            }

            // Failure effects are intentionally ignored at scoring time. Expected-value
            // weighting by roll probability is deferred until a risk-aware pass exists.

            AccumulateNode(node.onSuccessChain, context, target, targetIsEnemy, ref utility);
        }

        private static void AccumulateInvocation(IEffectInvocation invocation, VehicleAISharedContext context, bool targetIsEnemy, ref CommandWeights utility)
        {
            switch (invocation)
            {
                case EntityEffectInvocation e when e.effect != null:
                    AccumulateEntityEffect(e.effect, targetIsEnemy, ref utility);
                    break;
                case VehicleEffectInvocation v when v.effect != null:
                    AccumulateVehicleEffect(v.effect, context, targetIsEnemy, ref utility);
                    break;
                // SeatEffectInvocation handling is deferred to Phase 2 alongside role-specific effects.
            }
        }

        // ==================== ENTITY EFFECTS ====================

        private static void AccumulateEntityEffect(IEntityEffect effect, bool targetIsEnemy, ref CommandWeights utility)
        {
            switch (effect)
            {
                case DamageEffect dmg:
                    utility.attack += ScoreDamage(dmg, targetIsEnemy);
                    break;

                case ResourceRestorationEffect res:
                    utility.heal += ScoreRestoration(res.formula, targetIsEnemy);
                    break;

                case EntityModifierEffect mod:
                    AccumulateModifier(mod.value, targetIsEnemy, ref utility);
                    break;

                case ApplyEntityConditionEffect _:
                    if (targetIsEnemy) utility.disrupt += 0.5f;
                    else utility.heal += 0.3f;
                    break;

                case RemoveEntityConditionByCategoryEffect _:
                case RemoveEntityConditionByTemplateEffect _:
                    if (!targetIsEnemy) utility.heal += 0.4f;
                    break;
            }
        }

        // ==================== VEHICLE EFFECTS ====================

        private static void AccumulateVehicleEffect(IVehicleEffect effect, VehicleAISharedContext context, bool targetIsEnemy, ref CommandWeights utility)
        {
            switch (effect)
            {
                case DamageEffect dmg:
                    utility.attack += ScoreDamage(dmg, targetIsEnemy);
                    break;

                case ResourceRestorationEffect res:
                    utility.heal += ScoreRestoration(res.formula, targetIsEnemy);
                    break;

                case EntityModifierEffect mod:
                    AccumulateModifier(mod.value, targetIsEnemy, ref utility);
                    break;

                case ApplyVehicleConditionEffect _:
                    if (targetIsEnemy) utility.disrupt += 0.6f;
                    else utility.heal += 0.4f;
                    break;

                case RemoveVehicleConditionByCategoryEffect _:
                case RemoveVehicleConditionByTemplateEffect _:
                    if (!targetIsEnemy) utility.heal += 0.5f;
                    break;

                case ProgressModifierEffect prog:
                    AccumulateProgress(prog, targetIsEnemy, ref utility);
                    break;

                case SetSpeedEffect speed:
                    float delta = (speed.targetSpeedPercent / 100f) - context.SpeedPercent;
                    if (targetIsEnemy)
                        utility.disrupt += Mathf.Clamp(-delta, -1f, 1f);
                    else
                        utility.attack += Mathf.Max(0f, delta) * 0.3f;
                    break;

                case RelativeLaneChangeEffect _:
                case AbsoluteLaneChangeEffect _:
                    // Lane utility depends on hazard context — deferred until LaneHazardTracker exists.
                    break;

                case RestoreConsumableEffect _:
                    if (!targetIsEnemy) utility.heal += 0.3f;
                    break;
            }
        }

        // ==================== HELPERS ====================

        private static float ScoreDamage(DamageEffect dmg, bool targetIsEnemy)
        {
            // Damage aimed at self or an ally is an anti-utility for the attack axis.
            if (!targetIsEnemy) return -0.5f;

            DamageFormula formula = null;
            if (dmg.formulaProvider is StaticFormulaProvider staticProvider)
                formula = staticProvider.formula;

            if (formula == null)
            {
                // Weapon-derived formulas depend on runtime weapon data — give a neutral positive
                // until a weapon-aware expected-damage path exists.
                return 0.6f;
            }

            float expected = formula.baseDice * (formula.dieSize + 1) / 2f + formula.bonus;
            return Mathf.Clamp01(expected / DamageSaturation);
        }

        private static float ScoreRestoration(RestorationFormula formula, bool targetIsEnemy)
        {
            if (formula == null) return 0f;

            float expected = formula.baseDice * (formula.dieSize + 1) / 2f + formula.bonus;
            float normalised = Mathf.Clamp01(expected / RestorationSaturation);

            if (formula.isDrain)
                return targetIsEnemy ? normalised : -normalised;

            return targetIsEnemy ? -normalised : normalised;
        }

        private static void AccumulateModifier(float value, bool targetIsEnemy, ref CommandWeights utility)
        {
            float scaled = Mathf.Clamp(value * 0.05f, -1f, 1f);
            // Debuffing an enemy or buffing an ally is positive; the inverse is negative.
            if (targetIsEnemy) utility.disrupt += -scaled;
            else utility.heal += scaled;
        }

        private static void AccumulateProgress(ProgressModifierEffect prog, bool targetIsEnemy, ref CommandWeights utility)
        {
            if (prog.mode != ProgressModifierEffect.ProgressModifierMode.Flat) return;

            float scaled = Mathf.Clamp(prog.amount / ProgressSaturation, -1f, 1f);
            if (targetIsEnemy)
                utility.disrupt += -scaled;      // pushing an enemy back = positive disrupt
            else if (scaled < 0f)
                utility.flee += -scaled;          // pushing self back is a niche flee utility
            else
                utility.attack += scaled * 0.3f;  // pushing self forward is a mild attack/pressure utility
        }

        private static bool IsEnemy(VehicleAISharedContext context, IRollTarget target)
        {
            if (context == null || context.Self == null || target == null) return false;
            Vehicle targetVehicle = EntityHelpers.GetVehicleFromTarget(target);
            if (targetVehicle == null) return false;
            return TurnService.AreHostile(context.Self, targetVehicle);
        }
    }
}
