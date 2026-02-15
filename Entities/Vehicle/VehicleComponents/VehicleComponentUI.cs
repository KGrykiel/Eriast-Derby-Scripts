public static class VehicleComponentUI
{
    public struct DisplayStat
    {
        public string Name;
        public string Label;
        public string Value;
        public float? Current;
        public float? Max;
        public bool ShowBar;
        public Attribute? Attribute;
        public int BaseValue;
        public int FinalValue;
        
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
