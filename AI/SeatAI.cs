using System.Collections.Generic;
using Assets.Scripts.AI.Perception;
using Assets.Scripts.AI.Personality;
using Assets.Scripts.AI.Scoring;
using Assets.Scripts.Characters;
using Assets.Scripts.Entities;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Entities.Vehicles.VehicleComponents;
using Assets.Scripts.Skills;
using UnityEngine;

namespace Assets.Scripts.AI
{
    /// <summary>
    /// Per-seat decision maker. Runs the full four-stage pipeline:
    /// Perception (trackers) → Scoring (CommandWeights + SkillScorer) →
    /// Selection (ArgMax over (skill, target) pairs) → Execution (SkillPipeline).
    ///
    /// One <see cref="SkillAction"/> is built per action the seat spends; actions
    /// are chosen greedily until no positive-scoring action remains or the seat
    /// runs out of action economy. The action object never leaves this class.
    /// </summary>
    public class SeatAI
    {
        private readonly VehicleSeat seat;
        private readonly List<ITracker> trackers;

        public SeatAI(VehicleSeat seat)
        {
            this.seat = seat;
            this.trackers = new List<ITracker>
            {
                new ThreatTracker(),
                new OpportunityTracker()
            };
        }

        public bool TryAct(VehicleAISharedContext context)
        {
            if (seat == null || !seat.CanAct())
            {
                AILogManager.LogSeatSkipped(seat, context, seat != null ? "CanAct() returned false" : "seat is null");
                return false;
            }

            // ---- Perception ----
            PerceptionReadings perception = RunTrackers(context);

            // ---- Scoring: build the command weight vector ----
            PersonalityProfile personality = ResolvePersonality();
            CommandWeights weights = BuildCommandWeights(perception, context, personality);

            // ---- Selection ----
            var candidates = new List<(Skill skill, IRollTarget target, float score)>();
            SkillAction best = SelectBestAction(context, weights, candidates);

            // ---- Log before execution ----
            // The AI decision entry always precedes the combat entries it causes.
            AILogManager.TakenAction? takenAction = best != null
                ? new AILogManager.TakenAction(best.skill, best.target, candidates)
                : null;
            AILogManager.LogSeatAction(seat, context, perception, weights, takenAction);

            // ---- Execution ----
            if (best != null)
                SkillPipeline.Execute(best);

            return best != null;
        }

        // ==================== PIPELINE STAGES ====================

        private PersonalityProfile ResolvePersonality()
        {
            Character character = seat.AssignedCharacter;
            if (character != null && character.personality != null)
                return character.personality;
            return new PersonalityProfile();
        }

        private PerceptionReadings RunTrackers(VehicleAISharedContext context)
        {
            var readings = new PerceptionReadings();
            foreach (var tracker in trackers)
                readings.Set(tracker.GetType(), tracker.Evaluate(context));
            return readings;
        }

        private static CommandWeights BuildCommandWeights(PerceptionReadings perception, VehicleAISharedContext ctx, PersonalityProfile p)
        {
            float threat = perception.Get<ThreatTracker>();
            float opportunity = perception.Get<OpportunityTracker>();
            float lowHealthDrive = 1f - Mathf.Clamp01(ctx.ChassisHealthPercent);

            return new CommandWeights
            {
                attack = opportunity * p.aggression,
                heal = lowHealthDrive * p.defensiveness,
                disrupt = opportunity * 0.5f * p.aggression,
                flee = threat * lowHealthDrive * p.caution
            };
        }

        private SkillAction SelectBestAction(VehicleAISharedContext ctx, CommandWeights weights, List<(Skill skill, IRollTarget target, float score)> candidates)
        {
            List<Skill> skills = seat.GetAvailableSkills();
            SkillAction best = null;
            float bestScore = 0f;

            foreach (var skill in skills)
            {
                if (skill == null) continue;
                if (!seat.CanSpendAction(skill.actionCost)) continue;

                foreach (IRollTarget target in EnumerateTargets(ctx, skill))
                {
                    if (target == null) continue;

                    float score = SkillScorer.Score(skill, weights, ctx, target);
                    if (score > 0f)
                        candidates.Add((skill, target, score));
                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = new SkillAction(skill, seat.BuildActorForSkill(skill), target);
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
                    yield return ctx.Self;
                    break;

                case TargetingMode.SourceComponent:
                {
                    bool anyTargetable = false;
                    foreach (var component in EnumerateTargetableComponents(ctx.Self, skill))
                    {
                        anyTargetable = true;
                        yield return component;
                    }
                    if (!anyTargetable)
                        Debug.LogWarning($"[SeatAI] Skill '{skill.name}' has TargetingMode.SourceComponent but no targetable components were found on '{ctx.Self.vehicleName}'.");
                    break;
                }

                case TargetingMode.OwnLane:
                    yield return ctx.CurrentLane;
                    break;

                case TargetingMode.Enemy:
                    foreach (var enemy in ctx.EnemiesInStage) yield return enemy;
                    break;

                case TargetingMode.EnemyComponent:
                    foreach (var enemy in ctx.EnemiesInStage)
                    {
                        bool anyTargetable = false;
                        foreach (var component in EnumerateTargetableComponents(enemy, skill))
                        {
                            anyTargetable = true;
                            yield return component;
                        }
                        if (!anyTargetable)
                            Debug.LogWarning($"[SeatAI] Skill '{skill.name}' has TargetingMode.EnemyComponent but no targetable components were found on '{enemy.vehicleName}'.");
                    }
                    break;

                case TargetingMode.Any:
                    yield return ctx.Self;
                    foreach (var ally in ctx.AlliesInStage) yield return ally;
                    foreach (var enemy in ctx.EnemiesInStage) yield return enemy;
                    break;

                case TargetingMode.AnyComponent:
                    foreach (var vehicle in AnyVehicles(ctx))
                    {
                        bool anyTargetable = false;
                        foreach (var component in EnumerateTargetableComponents(vehicle, skill))
                        {
                            anyTargetable = true;
                            yield return component;
                        }
                        if (!anyTargetable)
                            Debug.LogWarning($"[SeatAI] Skill '{skill.name}' has TargetingMode.AnyComponent but no targetable components were found on '{vehicle.vehicleName}'.");
                    }
                    break;

                case TargetingMode.Lane:
                    // Deferred until LaneHazardTracker exists — see AI-new.md Phase 4.
                    break;
            }
        }

        private IEnumerable<Vehicle> AnyVehicles(VehicleAISharedContext ctx)
        {
            yield return ctx.Self;
            foreach (var ally in ctx.AlliesInStage) yield return ally;
            foreach (var enemy in ctx.EnemiesInStage) yield return enemy;
        }

        private static IEnumerable<VehicleComponent> EnumerateTargetableComponents(Vehicle vehicle, Skill skill)
        {
            if (vehicle == null) yield break;
            foreach (var component in vehicle.AllComponents)
            {
                if (component != null && component.CanBeTargeted())
                    yield return component;
            }
        }
    }
}