/// <summary>
/// UI utilities and data structures for VehicleComponent display.
/// Separates UI concerns from component business logic.
/// </summary>
public static class VehicleComponentUI
{
    /// <summary>
    /// Represents a stat to display in the UI.
    /// Used by components to define how their stats should be displayed.
    /// INTEGER-FIRST DESIGN: BaseValue and FinalValue are integers (D&D discrete stats).
    /// </summary>
    public struct DisplayStat
    {
        public string Name;           // Internal name (e.g., "Speed")
        public string Label;          // Display label (e.g., "SPD")
        public string Value;          // Formatted value string (e.g., "10", "2d8+3")
        public float? Current;        // For bar display: current value
        public float? Max;            // For bar display: max value
        public bool ShowBar;          // Whether to show as a bar
        public Attribute? Attribute;  // Optional attribute for tooltip support
        public int BaseValue;         // Base value (for tooltip modifier display) - INTEGER
        public int FinalValue;        // Final value after modifiers (for tooltip) - INTEGER
        
        /// <summary>
        /// Create a simple stat display (just a value, no tooltip).
        /// INTEGER-FIRST: Direct integer input.
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
        /// INTEGER-FIRST: Direct integer input, no casting.
        /// </summary>
        public static DisplayStat WithTooltip(string name, string label, Attribute attribute, int baseValue, int finalValue, string suffix = "")
        {
            return new DisplayStat
            {
                Name = name,
                Label = label,
                Value = $"{finalValue}{suffix}",
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
        /// INTEGER-FIRST: Direct integer input.
        /// </summary>
        public static DisplayStat Bar(string name, string label, int current, int max)
        {
            return new DisplayStat
            {
                Name = name,
                Label = label,
                Value = $"{current}/{max}",
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
        /// INTEGER-FIRST: Direct integer input.
        /// </summary>
        public static DisplayStat BarWithTooltip(string name, string label, Attribute attribute, int current, int baseMax, int finalMax)
        {
            return new DisplayStat
            {
                Name = name,
                Label = label,
                Value = $"{current}/{finalMax}",
                Current = current,
                Max = finalMax,
                ShowBar = true,
                Attribute = attribute,
                BaseValue = baseMax,
                FinalValue = finalMax
            };
        }
    }
}
