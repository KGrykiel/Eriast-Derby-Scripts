using System.Collections.Generic;
using System.Text;
using Assets.Scripts.AI.Perception;
using Assets.Scripts.AI.Scoring;
using Assets.Scripts.Entities;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Logging;
using Assets.Scripts.Skills;
using EventType = Assets.Scripts.Logging.EventType;

namespace Assets.Scripts.AI
{
    /// <summary>
    /// Logging hub for AI pipeline events. Follows the same pattern as
    /// <see cref="Combat.Logging.CombatLogManager"/> and
    /// <see cref="EntityLogManager"/> — one entry per
    /// seat turn, emitted at <see cref="EventImportance.Debug"/> so it is hidden
    /// by default. The full pipeline detail (perception, weights, scoring table)
    /// lives in the <c>aiDecision</c> metadata key for hover tooltips.
    ///
    /// All methods are no-ops when inputs are null or degenerate.
    /// </summary>
    public static class AILogManager
    {
        /// <summary>
        /// Carries everything produced during one greedy-loop iteration: the action
        /// that was chosen and the full candidate list that was scored.
        /// </summary>
        public readonly struct TakenAction
        {
            public readonly Skill Skill;
            public readonly IRollTarget Target;
            public readonly List<(Skill skill, IRollTarget target, float score)> Candidates;

            public TakenAction(Skill skill, IRollTarget target, List<(Skill skill, IRollTarget target, float score)> candidates)
            {
                Skill      = skill;
                Target     = target;
                Candidates = candidates;
            }
        }

        // ==================== SEAT IDLE ====================

        /// <summary>
        /// One entry logged when a seat is skipped because <c>CanAct()</c> returned
        /// false before the pipeline began.
        /// </summary>
        public static void LogSeatSkipped(VehicleSeat seat, VehicleAISharedContext ctx, string reason)
        {
            if (seat == null || ctx == null || ctx.Self == null) return;

            RaceHistory.Log(
                EventType.AI,
                EventImportance.Debug,
                $"[AI] {FormatSeat(seat)} skipped - {reason}",
                ctx.CurrentStage,
                ctx.Self);
        }

        // ==================== SEAT TURN ====================

        /// <summary>
        /// One entry per seat turn. The message line summarises the outcome; the full
        /// pipeline detail (perception signals, command weights, per-action scoring
        /// tables) is packed into the <c>aiDecision</c> metadata key for tooltips.
        /// </summary>
        public static void LogSeatAction(
            VehicleSeat seat,
            VehicleAISharedContext ctx,
            PerceptionReadings perception,
            CommandWeights weights,
            TakenAction? action)
        {
            if (seat == null || ctx == null || ctx.Self == null) return;

            string summary = BuildSummaryLine(seat, action);
            var logEvt = RaceHistory.Log(
                EventType.AI,
                EventImportance.Debug,
                summary,
                ctx.CurrentStage,
                ctx.Self);

            logEvt.WithMetadata("aiDecision", BuildTooltip(ctx, perception, weights, action));
            logEvt.WithMetadata("seatName", seat.seatName);
            logEvt.WithMetadata("actionTaken", action.HasValue);
        }

        // ==================== SUMMARY LINE ====================

        private static string BuildSummaryLine(VehicleSeat seat, TakenAction? action)
        {
            string seatLabel = FormatSeat(seat);

            if (action == null)
                return $"{seatLabel} - no viable action";

            string skillName  = action.Value.Skill != null ? $"{LogColors.Ability(action.Value.Skill.name)}" : "?";
            string targetName = FormatTarget(action.Value.Target);
            return $"{seatLabel} -> {skillName} -> {targetName}";
        }

        // ==================== TOOLTIP ====================

        private static string BuildTooltip(
            VehicleAISharedContext ctx,
            PerceptionReadings perception,
            CommandWeights weights,
            TakenAction? action)
        {
            var sb = new StringBuilder();

            // ---- Perception ----
            sb.AppendLine("<b>Perception</b>");
            foreach (var kvp in perception.All)
                sb.AppendLine($"  {kvp.Key.Name}={kvp.Value:F2}");
            sb.AppendLine();

            // ---- Command weights ----
            sb.AppendLine("<b>Command Weights</b>");
            sb.AppendLine($"  attack={weights.attack:F2}  heal={weights.heal:F2}  disrupt={weights.disrupt:F2}  flee={weights.flee:F2}");

            // ---- Scoring table ----
            sb.AppendLine();
            if (action == null)
            {
                sb.AppendLine("<b>Result</b>  no viable action (all scores <= 0 or no valid targets)");
            }
            else
            {
                sb.AppendLine("<b>Scoring</b>");
                AppendScoringTable(sb, action.Value.Candidates, action.Value.Skill, action.Value.Target);
            }

            return sb.ToString().TrimEnd();
        }

        private static void AppendScoringTable(
            StringBuilder sb,
            List<(Skill skill, IRollTarget target, float score)> candidates,
            Skill selected,
            IRollTarget selectedTarget)
        {
            if (candidates == null || candidates.Count == 0)
            {
                sb.AppendLine("  (no candidates scored above zero)");
                return;
            }

            // Collapse to one row per skill — best-scoring target for each.
            var bestPerSkill = new Dictionary<Skill, (IRollTarget target, float score)>();
            foreach (var (skill, tgt, score) in candidates)
            {
                if (skill == null) continue;
                if (!bestPerSkill.TryGetValue(skill, out var current) || score > current.score)
                    bestPerSkill[skill] = (tgt, score);
            }

            var rows = new List<(Skill skill, IRollTarget target, float score)>();
            foreach (var kvp in bestPerSkill)
                rows.Add((kvp.Key, kvp.Value.target, kvp.Value.score));

            rows.Sort((a, b) => b.score.CompareTo(a.score));

            foreach (var (skill, tgt, score) in rows)
            {
                bool isSelected = skill == selected && tgt == selectedTarget;
                string prefix    = isSelected ? "  > " : "    ";
                string skillStr  = $"{LogColors.Ability(skill.name)}";
                string targetStr = FormatTarget(tgt);
                sb.AppendLine($"{prefix}{skillStr} -> {targetStr}  {score:F3}");
            }
        }

        // ==================== FORMATTING HELPERS ====================

        private static string FormatSeat(VehicleSeat seat)
        {
            string vehicleName = seat.ParentVehicle != null ? seat.ParentVehicle.vehicleName : "?";
            return $"{LogColors.Vehicle(vehicleName)}/{seat.seatName}";
        }

        private static string FormatTarget(IRollTarget target)
        {
            if (target == null) return "?";
            if (target is Vehicle v) return $"{LogColors.Vehicle(v.vehicleName)}";
            if (target is Entity e) return $"{LogColors.Vehicle(e.name)}";
            return target.GetType().Name;
        }
    }
}


