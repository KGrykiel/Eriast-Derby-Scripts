using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class SimulationMonitor : MonoBehaviour
{
    public GameManager gameManager; // Assign in Inspector

    [Header("Assign in Inspector")]
    public TextMeshProUGUI simulationLogText;
    public TextMeshProUGUI vehicleInfoText;
    public TMP_Dropdown vehicleDropdown;

    private Vehicle selectedVehicle = null;
    private List<Vehicle> vehicles = new List<Vehicle>();

    void Start()
    {
        if (gameManager != null)
            vehicles = gameManager.GetVehicles();

        SetupDropdown();

        // Optionally, select the first vehicle by default
        if (vehicles.Count > 0)
            SetSelectedVehicle(vehicles[0]);
        RefreshAll();
    }

    void Update()
    {
        RefreshAll();
    }

    private void SetupDropdown()
    {
        if (vehicleDropdown == null || vehicles == null) return;

        vehicleDropdown.ClearOptions();
        List<string> options = new List<string>();
        foreach (var v in vehicles)
            options.Add(v.vehicleName);

        vehicleDropdown.AddOptions(options);
        vehicleDropdown.onValueChanged.RemoveAllListeners();
        vehicleDropdown.onValueChanged.AddListener(OnDropdownChanged);
        vehicleDropdown.value = vehicles.IndexOf(selectedVehicle);
    }

    private void OnDropdownChanged(int index)
    {
        if (vehicles != null && index >= 0 && index < vehicles.Count)
        {
            SetSelectedVehicle(vehicles[index]);
        }
    }

    public void SetSelectedVehicle(Vehicle v)
    {
        selectedVehicle = v;
        RefreshVehicleInfo();
        if (vehicleDropdown != null && vehicles != null)
        {
            int idx = vehicles.IndexOf(v);
            if (idx >= 0 && vehicleDropdown.value != idx)
                vehicleDropdown.value = idx;
        }
    }

    private void RefreshAll()
    {
        RefreshSimulationLog();
        RefreshVehicleInfo();
    }

    private void RefreshSimulationLog()
    {
        if (simulationLogText == null) return;
        var log = SimulationLogger.Log;
        simulationLogText.text = string.Join("\n<color=#888888>──────────────</color>\n", log);
    }

    private void RefreshVehicleInfo()
    {
        if (vehicleInfoText == null || selectedVehicle == null) return;

        List<string> lines = new List<string>
        {
            $"<b>Name:</b> {selectedVehicle.vehicleName}",
            $"<b>Status:</b> {selectedVehicle.Status}",
            $"<b>Stage:</b> {(selectedVehicle.currentStage != null ? selectedVehicle.currentStage.stageName : "None")}",
            $"<b>MaxHealth:</b> {selectedVehicle.GetAttribute(Attribute.MaxHealth):0.0}",
            $"<b>Speed:</b> {selectedVehicle.GetAttribute(Attribute.Speed):0.0}",
            $"<b>Armor Class:</b> {selectedVehicle.GetAttribute(Attribute.ArmorClass):0.0}",
            $"<b>MaxEnergy:</b> {selectedVehicle.GetAttribute(Attribute.MaxEnergy):0.0}",
            $"<b>Current Health:</b> {selectedVehicle.health}",
            $"<b>Current Energy:</b> {selectedVehicle.energy}",
            "<b>Other Attributes:</b>"
        };

        foreach (var attr in System.Enum.GetValues(typeof(Attribute)))
        {
            if ((Attribute)attr != Attribute.MaxHealth &&
                (Attribute)attr != Attribute.Speed &&
                (Attribute)attr != Attribute.ArmorClass &&
                (Attribute)attr != Attribute.MaxEnergy)
            {
                lines.Add($"{attr}: {selectedVehicle.GetAttribute((Attribute)attr):0.0}");
            }
        }

        lines.Add("<b>Active Modifiers:</b>");
        foreach (var mod in selectedVehicle.GetActiveModifiers())
        {
            string modDesc = $"{mod.Type} {mod.Attribute} {mod.Value} (Src: {(mod.Source != null ? mod.Source.name : "None")}, Turns: {mod.DurationTurns})";
            lines.Add(modDesc);
        }

        vehicleInfoText.text = string.Join("\n", lines);
    }
}
