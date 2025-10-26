using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages player input, UI interactions, and turn execution.
/// Handles skill selection, target selection, and stage choice UI.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("UI References")]
    public Transform skillButtonContainer;
    public Button skillButtonPrefab;
    public GameObject targetSelectionPanel;
    public Transform targetButtonContainer;
    public Button targetButtonPrefab;
    public Button targetCancelButton;
    public GameObject stageSelectionPanel;
    public Transform stageButtonContainer;
    public Button stageButtonPrefab;
    public Button nextTurnButton;

    private TurnController turnController;
    private Vehicle playerVehicle;
    private System.Action onPlayerTurnComplete;

    // Player selection state
    private Vehicle selectedTarget = null;
    private Skill selectedSkill = null;
    private int selectedSkillIndex = -1;
    private bool isSelectingStage = false;

    // UI button caches
    private List<Button> skillButtons = new List<Button>();
    private List<Button> stageButtons = new List<Button>();

    /// <summary>
    /// Initializes the player controller with required references.
    /// </summary>
    /// <param name="player">The player's vehicle</param>
    /// <param name="controller">Reference to TurnController</param>
    /// <param name="turnCompleteCallback">Callback invoked when player turn completes</param>
    public void Initialize(Vehicle player, TurnController controller, System.Action turnCompleteCallback)
    {
        playerVehicle = player;
        turnController = controller;
        onPlayerTurnComplete = turnCompleteCallback;

        if (targetCancelButton != null)
            targetCancelButton.onClick.AddListener(OnTargetCancelClicked);
    }

    /// <summary>
    /// Returns true if the player is currently making a decision (stage selection).
    /// </summary>
    public bool IsAwaitingInput => isSelectingStage;

    #region Player Movement

    /// <summary>
    /// Processes player movement through stages.
    /// Automatically moves through linear paths, pauses at crossroads for player choice.
    /// </summary>
    public void ProcessPlayerMovement()
    {
        if (isSelectingStage) return;

        while (playerVehicle.progress >= playerVehicle.currentStage.length)
        {
            if (playerVehicle.currentStage.nextStages.Count == 0)
            {
                break;
            }
            else if (playerVehicle.currentStage.nextStages.Count == 1)
            {
                turnController.MoveToStage(playerVehicle, playerVehicle.currentStage.nextStages[0]);
            }
            else
            {
                isSelectingStage = true;
                ShowStageSelection(playerVehicle.currentStage.nextStages);
                return;
            }
        }

        SimulationLogger.LogEvent($"It's now {playerVehicle.vehicleName}'s (Player) turn.");
        ShowSkillSelection();
    }

    #endregion

    #region Skill Selection UI

    /// <summary>
    /// Displays skill selection UI with buttons for each available skill.
    /// </summary>
    private void ShowSkillSelection()
    {
        if (skillButtonContainer == null || skillButtonPrefab == null || playerVehicle == null) return;

        while (skillButtons.Count < playerVehicle.skills.Count)
        {
            Button btn = Instantiate(skillButtonPrefab, skillButtonContainer);
            skillButtons.Add(btn);
        }

        for (int i = 0; i < skillButtons.Count; i++)
        {
            if (i < playerVehicle.skills.Count)
            {
                skillButtons[i].gameObject.SetActive(true);
                skillButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = playerVehicle.skills[i].name;
                int skillIndex = i;
                skillButtons[i].onClick.RemoveAllListeners();
                skillButtons[i].onClick.AddListener(() => OnSkillButtonClicked(skillIndex));
            }
            else
            {
                skillButtons[i].gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Handles skill button click. Shows target selection if skill requires a target.
    /// </summary>
    private void OnSkillButtonClicked(int skillIndex)
    {
        selectedSkill = playerVehicle.skills[skillIndex];
        selectedSkillIndex = skillIndex;
        SimulationLogger.LogEvent($"Player selected skill: {selectedSkill.name}");

        bool needsTarget = SkillNeedsTarget(selectedSkill);
        if (needsTarget)
        {
            ShowTargetSelection();
        }
        else
        {
            selectedTarget = null;
        }
    }

    /// <summary>
    /// Checks if a skill requires target selection based on its effect invocations.
    /// </summary>
    private bool SkillNeedsTarget(Skill skill)
    {
        if (skill.effectInvocations == null) return false;

        foreach (var invocation in skill.effectInvocations)
        {
            if (invocation.targetMode == EffectTargetMode.Target ||
                invocation.targetMode == EffectTargetMode.Both)
                return true;
        }
        return false;
    }

    #endregion

    #region Target Selection UI

    /// <summary>
    /// Displays target selection UI with buttons for each valid target.
    /// Only shows targets in the same stage and currently active.
    /// </summary>
    private void ShowTargetSelection()
    {
        if (targetSelectionPanel == null || targetButtonContainer == null || targetButtonPrefab == null)
            return;

        targetSelectionPanel.SetActive(true);

        foreach (Transform child in targetButtonContainer)
            Destroy(child.gameObject);

        List<Vehicle> validTargets = turnController.GetValidTargets(playerVehicle);

        if (validTargets.Count == 0)
        {
            SimulationLogger.LogEvent("No valid targets available.");
            targetSelectionPanel.SetActive(false);
            return;
        }

        foreach (var v in validTargets)
        {
            Button btn = Instantiate(targetButtonPrefab, targetButtonContainer);
            btn.GetComponentInChildren<TextMeshProUGUI>().text = v.vehicleName;
            btn.onClick.AddListener(() => OnTargetButtonClicked(v));
        }
    }

    /// <summary>
    /// Handles target button click. Closes target selection panel.
    /// </summary>
    private void OnTargetButtonClicked(Vehicle v)
    {
        selectedTarget = v;
        if (targetSelectionPanel != null)
            targetSelectionPanel.SetActive(false);
    }

    /// <summary>
    /// Handles target cancel button. Clears all selections and closes panel.
    /// </summary>
    private void OnTargetCancelClicked()
    {
        selectedTarget = null;
        selectedSkill = null;
        selectedSkillIndex = -1;
        if (targetSelectionPanel != null)
            targetSelectionPanel.SetActive(false);
    }

    #endregion

    #region Stage Selection UI

    /// <summary>
    /// Displays stage selection UI when player reaches a crossroads.
    /// </summary>
    private void ShowStageSelection(List<Stage> options)
    {
        if (stageSelectionPanel == null || stageButtonContainer == null || stageButtonPrefab == null)
            return;

        stageSelectionPanel.SetActive(true);
        if (nextTurnButton != null)
            nextTurnButton.interactable = false;

        while (stageButtons.Count < options.Count)
        {
            Button btn = Instantiate(stageButtonPrefab, stageButtonContainer);
            stageButtons.Add(btn);
        }

        for (int i = 0; i < stageButtons.Count; i++)
        {
            if (i < options.Count)
            {
                stageButtons[i].gameObject.SetActive(true);
                stageButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = options[i].stageName;
                Stage stage = options[i];
                stageButtons[i].onClick.RemoveAllListeners();
                stageButtons[i].onClick.AddListener(() => OnStageButtonClicked(stage));
            }
            else
            {
                stageButtons[i].gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Handles stage button click. Moves player to selected stage and continues movement processing.
    /// </summary>
    private void OnStageButtonClicked(Stage selectedStage)
    {
        stageSelectionPanel.SetActive(false);
        isSelectingStage = false;

        if (nextTurnButton != null)
            nextTurnButton.interactable = true;

        turnController.MoveToStage(playerVehicle, selectedStage);
        ProcessPlayerMovement();
    }

    #endregion

    #region Player Action Execution

    /// <summary>
    /// Executes the player's selected skill and ends their turn.
    /// Called by the Next Turn button in the UI.
    /// Validates target selection and energy cost before execution.
    /// </summary>
    public void ExecuteSkillAndEndTurn()
    {
        if (isSelectingStage) return;

        Vehicle target = playerVehicle;

        if (selectedSkill != null)
        {
            bool needsTarget = SkillNeedsTarget(selectedSkill);

            if (needsTarget)
            {
                target = selectedTarget;
                if (target == null)
                {
                    SimulationLogger.LogEvent("Player skipped turn (no target selected).");
                    ClearPlayerSelections();
                    onPlayerTurnComplete?.Invoke();
                    return;
                }
            }

            if (playerVehicle.energy < selectedSkill.energyCost)
            {
                SimulationLogger.LogEvent($"{playerVehicle.vehicleName} does not have enough energy to use {selectedSkill.name}.");
                return;
            }

            bool result = selectedSkill.Use(playerVehicle, target);
            string targetName = GetSkillMainTargetName(selectedSkill, target);

            if (result)
            {
                playerVehicle.energy -= selectedSkill.energyCost;
                SimulationLogger.LogEvent($"Player used skill: {selectedSkill.name} on {targetName} (Success)");
            }
            else
            {
                SimulationLogger.LogEvent($"Player used skill: {selectedSkill.name} on {targetName} (Failed)");
            }
        }
        else
        {
            SimulationLogger.LogEvent("Player skipped their turn (no skill selected).");
        }

        ClearPlayerSelections();
        onPlayerTurnComplete?.Invoke();
    }

    /// <summary>
    /// Generates a display name for the main target based on skill targeting mode.
    /// </summary>
    private string GetSkillMainTargetName(Skill skill, Vehicle mainTarget)
    {
        if (skill.effectInvocations != null && skill.effectInvocations.Exists(ei => ei.targetMode == EffectTargetMode.AllInStage))
            return "all vehicles in stage";
        if (skill.effectInvocations != null && skill.effectInvocations.Exists(ei => ei.targetMode == EffectTargetMode.Both))
            return $"{playerVehicle.vehicleName} and {mainTarget.vehicleName}";
        return mainTarget != null ? mainTarget.vehicleName : "none";
    }

    /// <summary>
    /// Clears all player selections (skill, target, stage choice).
    /// </summary>
    private void ClearPlayerSelections()
    {
        selectedSkill = null;
        selectedSkillIndex = -1;
        selectedTarget = null;
    }

    #endregion
}