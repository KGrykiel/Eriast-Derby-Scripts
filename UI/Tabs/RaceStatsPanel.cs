using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Scripts.Logging;
using Assets.Scripts.Managers;
using Assets.Scripts.Managers.Logging.Results;
using Assets.Scripts.Managers.Turn;
using Assets.Scripts.Statistics;
using TMPro;
using UnityEngine;

/// <summary>
/// DM overlay panel that displays live race statistics.
/// Refreshes at the end of each round and when the race ends.
/// </summary>
public class RaceStatsPanel : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The GameManager in the scene.")]
    [SerializeField] private GameManager gameManager;

    [Tooltip("Text element used to display the stats output.")]
    [SerializeField] private TextMeshProUGUI statsText;

    private RaceResult finalResult;

    // ==================== LIFECYCLE ====================

    private void OnEnable()
    {
        TurnEventBus.OnEvent += HandleTurnEvent;
        RefreshStats();
    }

    private void OnDisable()
    {
        TurnEventBus.OnEvent -= HandleTurnEvent;
    }

    // ==================== EVENT HANDLING ====================

    private void HandleTurnEvent(TurnEvent evt)
    {
        if (evt is TurnEndedEvent)
        {
            RefreshStats();
        }
        else if (evt is RaceOverEvent raceOver)
        {
            finalResult = raceOver.Result;
            RefreshStats();
        }
    }

    // ==================== REFRESH ====================

    private void RefreshStats()
    {
        if (statsText == null) return;
        if (gameManager == null) return;

        RaceStatsTracker tracker = gameManager.GetStatsTracker();
        if (tracker == null) return;

        var sm = gameManager.GetStateMachine();
        int currentRound   = sm != null ? sm.CurrentRound : 0;
        int vehiclesActive = sm != null ? sm.AllVehicles.Count : 0;
        int vehiclesTotal  = finalResult != null
            ? finalResult.TotalParticipants
            : vehiclesActive;

        RaceMetrics metrics = tracker.BuildSnapshot(currentRound, vehiclesTotal, vehiclesActive, finalResult);
        statsText.text = FormatMetrics(metrics);
    }

    // ==================== FORMATTING ====================

    private static string FormatMetrics(RaceMetrics m)
    {
        var sb = new StringBuilder();

        AppendRaceState(sb, m);
        AppendFinishOrder(sb, m);
        AppendEliminations(sb, m);
        AppendSkillUsage(sb, m);
        AppendCombat(sb, m);
        AppendSavingThrows(sb, m);
        AppendConditions(sb, m);
        AppendPerVehicle(sb, m);

        return sb.ToString().TrimEnd();
    }

    private static void AppendRaceState(StringBuilder sb, RaceMetrics m)
    {
        sb.AppendLine(Header("RACE STATE"));
        sb.AppendLine($"Round: {Num(m.CurrentRound.ToString())}");
        sb.AppendLine($"Active: {Num(m.VehiclesActive.ToString())}  |  "
                    + $"Finished: {Num(m.VehiclesFinished.ToString())}  |  "
                    + $"Eliminated: {Num(m.VehiclesEliminated.ToString())}");
    }

    private static void AppendFinishOrder(StringBuilder sb, RaceMetrics m)
    {
        if (m.FinishOrder.Count == 0) return;
        sb.AppendLine();
        sb.AppendLine(Header("FINISHING ORDER"));
        foreach (var (name, pos, round) in m.FinishOrder)
            sb.AppendLine($"{Num(pos.ToString())}. {VehicleName(name)}  {Dim($"(Round {round})")}");
    }

    private static void AppendEliminations(StringBuilder sb, RaceMetrics m)
    {
        if (m.Eliminations.Count == 0) return;
        sb.AppendLine();
        sb.AppendLine(Header("ELIMINATIONS"));
        foreach (var (name, stage, round) in m.Eliminations)
            sb.AppendLine($"- {VehicleName(name)}  {Dim($"{stage}, Round {round}")}");
    }

    private static void AppendSkillUsage(StringBuilder sb, RaceMetrics m)
    {
        if (m.SkillUseCounts.Count == 0) return;
        sb.AppendLine();
        sb.AppendLine(Header("SKILL USAGE (top 10)"));
        var top = m.SkillUseCounts.OrderByDescending(kv => kv.Value).Take(10);
        foreach (var kv in top)
            sb.AppendLine($"{SkillName(kv.Key),-40} {Num(kv.Value.ToString())}x");
    }

    private static void AppendCombat(StringBuilder sb, RaceMetrics m)
    {
        if (m.TotalAttacks == 0 && m.TotalDamageDealt == 0) return;
        int hitPct = m.TotalAttacks > 0 ? m.TotalHits * 100 / m.TotalAttacks : 0;
        sb.AppendLine();
        sb.AppendLine(Header("COMBAT"));
        sb.AppendLine($"Attacks: {Num(m.TotalAttacks.ToString())}   "
                    + $"Hits: {Good(m.TotalHits.ToString())} {Dim($"({hitPct}%)")}   "
                    + $"Misses: {Bad(m.TotalMisses.ToString())}");
        sb.AppendLine($"Nat 20s: {Good(m.TotalNat20s.ToString())}    Nat 1s: {Bad(m.TotalNat1s.ToString())}");
        sb.AppendLine($"Total Damage: {Num(m.TotalDamageDealt.ToString())}");
    }

    private static void AppendSavingThrows(StringBuilder sb, RaceMetrics m)
    {
        if (m.TotalSavingThrows == 0) return;
        int passPct = m.TotalSavingThrows > 0 ? m.SavesPassed * 100 / m.TotalSavingThrows : 0;
        sb.AppendLine();
        sb.AppendLine(Header("SAVING THROWS"));
        sb.AppendLine($"Total: {Num(m.TotalSavingThrows.ToString())}   "
                    + $"Passed: {Good(m.SavesPassed.ToString())} {Dim($"({passPct}%)")}   "
                    + $"Failed: {Bad(m.SavesFailed.ToString())}");
    }

    private static void AppendConditions(StringBuilder sb, RaceMetrics m)
    {
        if (m.ConditionApplyCounts.Count == 0) return;
        sb.AppendLine();
        sb.AppendLine(Header("CONDITIONS APPLIED (top 5)"));
        var top = m.ConditionApplyCounts.OrderByDescending(kv => kv.Value).Take(5);
        foreach (var kv in top)
            sb.AppendLine($"{CondName(kv.Key),-40} {Num(kv.Value.ToString())}x");
    }

    private static void AppendPerVehicle(StringBuilder sb, RaceMetrics m)
    {
        if (m.VehicleStats.Count == 0) return;
        sb.AppendLine();
        sb.AppendLine(Header("PER VEHICLE"));

        foreach (var kv in m.VehicleStats.OrderBy(k => k.Key))
        {
            var vm = kv.Value;
            int hitPct  = vm.Attacks       > 0 ? vm.Hits       * 100 / vm.Attacks       : 0;
            int passPct = vm.SavingThrows  > 0 ? vm.SavesPassed * 100 / vm.SavingThrows  : 0;

            sb.AppendLine();
            sb.AppendLine(VehicleName(vm.VehicleName));
            sb.AppendLine($"  Attacks: {Num(vm.Attacks.ToString())}  "
                        + $"Hits: {Good(vm.Hits.ToString())} {Dim($"({hitPct}%)")}  "
                        + $"Nat 20s: {Good(vm.Nat20s.ToString())}  "
                        + $"Nat 1s: {Bad(vm.Nat1s.ToString())}");
            sb.AppendLine($"  Damage Dealt: {Num(vm.DamageDealt.ToString())}  "
                        + $"Damage Received: {Num(vm.DamageReceived.ToString())}");
            sb.AppendLine($"  Saves: {Num(vm.SavingThrows.ToString())}  "
                        + $"Passed: {Good(vm.SavesPassed.ToString())} {Dim($"({passPct}%)")}  "
                        + $"Failed: {Bad(vm.SavesFailed.ToString())}");
            sb.AppendLine($"  Conditions Inflicted: {Num(vm.ConditionsInflicted.ToString())}  "
                        + $"Received: {Num(vm.ConditionsReceived.ToString())}");

            if (vm.SkillUseCounts.Count > 0)
            {
                var topSkills = vm.SkillUseCounts.OrderByDescending(s => s.Value).Take(5);
                string skillLine = string.Join(", ", topSkills.Select(s => $"{SkillName(s.Key)} {Num(s.Value.ToString())}x"));
                sb.AppendLine($"  Top Skills: {skillLine}");
            }
        }
    }

    // ==================== COLOUR HELPERS ====================

    private static string Header(string text)
        => $"<b><color={LogColors.InspectorHeader}>=== {text} ===</color></b>";

    private static string VehicleName(string text)
        => $"<b><color={LogColors.VehicleName}>{text}</color></b>";

    private static string SkillName(string text)
        => $"<color={LogColors.AbilityName}>{text}</color>";

    private static string CondName(string text)
        => $"<color={LogColors.ImportanceHigh}>{text}</color>";

    private static string Num(string text)
        => $"<color={LogColors.NumberColor}>{text}</color>";

    private static string Good(string text)
        => $"<color={LogColors.Available}>{text}</color>";

    private static string Bad(string text)
        => $"<color={LogColors.Warning}>{text}</color>";

    private static string Dim(string text)
        => $"<color={LogColors.FeedLocation}>{text}</color>";
}
