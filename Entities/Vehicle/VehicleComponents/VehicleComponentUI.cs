/// <summary>
/// UI utilities and data structures for VehicleComponent display.
/// Separates UI concerns from component business logic.
/// </summary>
public static class VehicleComponentUI
{
    /// <summary>
    /// Represents a stat to display in the UI.
    /// Used by components to define how their stats should be displayed.
    /// </summary>
    public struct DisplayStat
    {
        public string Name;           // Internal name (e.g., "Speed")
        public string Label;          // Display label (e.g., "SPD")
        public string Value;          // Formatted value string (e.g., "10.5", "2d8+3")
        public float? Current;        // For bar display: current value
        public float? Max;            // For bar display: max value
        public bool ShowBar;          // Whether to show as a bar
        public Attribute? Attribute;  // Optional attribute for tooltip support
        public float BaseValue;       // Base value (for tooltip modifier display)
        public float FinalValue;      // Final value after modifiers (for tooltip)
        
        /// <summary>
        /// Create a simple stat display (just a value, no tooltip).
        /// </summary>
        public static DisplayStat Simple(string name, string label, float value, string suffix = "")
        {
            return new DisplayStat
            {
                Name = name,
                Label = label,
                Value = $"{value:F1}{suffix}",
                Current = value,
                Max = null,
                ShowBar = false,
                Attribute = null,
                BaseValue = value,
                FinalValue = value
            };
        }
        
        /// <summary>
        /// Create a simple stat display with integer value (no tooltip).
        /// </summary>
        public static DisplayStat Simple(string name, string label, int value, string suffix = "")
        {
            return new DisplayStat
            {
                Name = name,
                Label = label,
                Value = $"{value}{suffix}",
                Current = value,
                Max = null,
                ShowBar = false,
                Attribute = null,
                BaseValue = value,
                FinalValue = value
            };
        }
        
        /// <summary>
        /// Create a simple stat display with string value (for dice notation, etc., no tooltip).
        /// </summary>
        public static DisplayStat Simple(string name, string label, string value)
        {
            return new DisplayStat
            {
                Name = name,
                Label = label,
                Value = value,
                Current = null,
                Max = null,
                ShowBar = false,
                Attribute = null,
                BaseValue = 0,
                FinalValue = 0
            };
        }
        
        /// <summary>
        /// Create a stat display with tooltip support (shows modifier breakdown on hover).
        /// </summary>
        public static DisplayStat WithTooltip(string name, string label, Attribute attribute, float baseValue, float finalValue, string suffix = "")
        {
            return new DisplayStat
            {
                Name = name,
                Label = label,
                Value = $"{finalValue:F1}{suffix}",
                Current = finalValue,
                Max = null,
                ShowBar = false,
                Attribute = attribute,
                BaseValue = baseValue,
                FinalValue = finalValue
            };
        }
        
        /// <summary>
        /// Create a bar stat display (current/max with optional bar, no tooltip).
        /// </summary>
        public static DisplayStat Bar(string name, string label, float current, float max)
        {
            return new DisplayStat
            {
                Name = name,
                Label = label,
                Value = $"{current:F0}/{max:F0}",
                Current = current,
                Max = max,
                ShowBar = true,
                Attribute = null,
                BaseValue = max,
                FinalValue = max
            };
        }
        
        /// <summary>
        /// Create a bar stat display with tooltip support.
        /// </summary>
        public static DisplayStat BarWithTooltip(string name, string label, Attribute attribute, float current, float baseMax, float finalMax)
        {
            return new DisplayStat
            {
                Name = name,
                Label = label,
                Value = $"{current:F0}/{finalMax:F0}",
                Current = current,
                Max = finalMax,
                ShowBar = true,
                Attribute = attribute,
                BaseValue = baseMax,
                FinalValue = finalMax
            };
        }
    }
    
    /// <summary>
    /// Get component status summary for debugging/UI.
    /// Utility method for generating debug strings.
    /// </summary>
    public static string GetStatusSummary(VehicleComponent component)
    {
        if (component == null) return "[NULL COMPONENT]";
        
        
        string status = $"<b>{component.name}</b> ({component.componentType})\n";
        status += $"HP: {component.health}/{component.maxHealth} | AC: {component.armorClass}\n";
        
        if (component.isDestroyed)
            status += "<color=red>[DESTROYED]</color>\n";
        else if (component.isDisabled)
            status += "<color=yellow>[DISABLED]</color>\n";
        
        if (component.roleType != RoleType.None)
            status += $"Enables: {component.roleType}\n";
        
        // Get character from seat that controls this component
        var seat = component.ParentVehicle?.GetSeatForComponent(component);
        if (seat?.assignedCharacter != null)
            status += $"Operated by: {seat.assignedCharacter.characterName}\n";
        
        if (component.componentSkills != null && component.componentSkills.Count > 0)
            status += $"Skills: {component.componentSkills.Count}\n";
        
        return status;
    }
}
