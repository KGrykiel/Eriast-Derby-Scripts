using System.Collections.Generic;
using Assets.Scripts.AI.Execution;
using Assets.Scripts.AI.Perception;
using Assets.Scripts.AI.Personality;
using Assets.Scripts.AI.Scoring;
using Assets.Scripts.Characters;
using Assets.Scripts.Combat.Rolls;
using Assets.Scripts.Entities;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Skills;
using UnityEngine;

namespace Assets.Scripts.AI
{
    /// <summary>
    /// Per-seat decision maker. Runs the full four-stage pipeline:
    /// Perception (trackers) → Scoring (CommandWeights + SkillScorer) →
    /// Selection (ArgMax over (skill, target) pairs) → Execution (SkillExecutor).
    ///
    /// One <see cref="AIAction"/> is built per action the seat spends; actions
    /// are chosen greedily until no positive-scoring action remains or the seat
    /// runs out of action economy. The action object never leaves this class.
    /// </summary>
    public class SeatAI
    {
        private readonly VehicleSeat seat;
        private readonly List<ITracker> trackers;
        private readonly SkillExecutor skillExecutor = new();

        public SeatAI(VehicleSeat seat)
        {
            this.seat = seat;
            this.trackers = new List<ITracker>
            {
                new ThreatTracker(),
                new OpportunityTracker()
            };
        }

        public void TakeTurn(VehicleAISharedContext context, TurnService turnService)
        {
            if (seat == null || !seat.CanAct()) return;

            // ---- Perception ----
            float threat = trackers[0].Evaluate(context);
            float opportunity = trackers[1].Evaluate(context);

            // ---- Scoring: build the command weight vector ----
            PersonalityProfile personality = ResolvePersonality();
            CommandWeights weights = BuildCommandWeights(threat, opportunity, context, personality);

            // ---- Selection + Execution loop ----
            // Greedy: each iteration picks the single best action the seat can still afford.
            // Exits when no positive-scoring action remains or action economy is exhausted.
            int safetyGuard = 8;
            while (safetyGuard-- > 0 && seat.HasAnyActionsRemaining() && seat.CanAct())
            {
                AIAction best = SelectBestAction(context, weights);
                if (best == null || best.score <= 0f) break;
                skillExecutor.Execute(best, turnService);
            }
        }

        // ==================== PIPELINE STAGES ====================

        private PersonalityProfile ResolvePersonality()
        {
            Character character = seat.AssignedCharacter;
            if (character != null && character.personality != null)
                return character.personality;
            return new PersonalityProfile();
        }

        private static CommandWeights BuildCommandWeights(float threat, float opportunity, VehicleAISharedContext ctx, PersonalityProfile p)
        {
            float lowHealthDrive = 1f - Mathf.Clamp01(ctx.ChassisHealthPercent);

            return new CommandWeights
            {
                attack  = opportunity * p.aggression,
                heal    = lowHealthDrive * p.defensiveness,
                disrupt = opportunity * 0.5f * p.aggression,
                flee    = threat * lowHealthDrive * p.caution
            };
        }

        private AIAction SelectBestAction(VehicleAISharedContext ctx, CommandWeights weights)
        {
            List<Skill> skills = seat.GetAvailableSkills();
            AIAction best = null;

            foreach (var skill in skills)
            {
                if (skill == null) continue;
                if (!seat.CanSpendAction(skill.actionCost)) continue;

                foreach (IRollTarget target in EnumerateTargets(ctx, skill))
                {
                    if (target == null) continue;

                    float score = SkillScorer.Score(skill, weights, ctx, target);
                    if (best == null || score > best.score)
                    {
                        RollActor actor = BuildActor(skill);
                        best = new AIAction(skill, target, actor, score);
                    }
                }
            }

            return best;
        }

        // ==================== TARGETING ====================

        private IEnumerable<IRollTarget> EnumerateTargets(VehicleAISharedContext ctx, Skill skill)
        {
            switch (skill.targetingMode)
            {
                case TargetingMode.Self:
                case TargetingMode.SourceComponent:
                    yield return ctx.Self;
                    break;

                case TargetingMode.OwnLane:
                    yield return ctx.CurrentLane;
                    break;

                case TargetingMode.Enemy:
                case TargetingMode.EnemyComponent:
                    foreach (var enemy in ctx.EnemiesInStage) yield return enemy;
                    break;

                case TargetingMode.Any:
                case TargetingMode.AnyComponent:
                    yield return ctx.Self;
                    foreach (var ally in ctx.AlliesInStage) yield return ally;
                    foreach (var enemy in ctx.EnemiesInStage) yield return enemy;
                    break;

                case TargetingMode.Lane:
                    // Deferred until LaneHazardTracker exists — see AI-new.md Phase 4.
                    break;
            }
        }

        private RollActor BuildActor(Skill skill)
        {
            var component = seat.GetComponentForSkill(skill);
            if (component != null) return new CharacterWithToolActor(seat, component);
            return new CharacterActor(seat);
        }
    }
}
