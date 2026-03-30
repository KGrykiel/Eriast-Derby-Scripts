using System;

namespace Assets.Scripts.Modifiers
{
    public interface IModifierSource
    {
        string ModifierLabel { get; }
    }

    public enum ModifierType
    {
        Flat,        // +X (additive)
        Multiplier,  // ×X (multiplicative, e.g., 1.5 = 150% = +50%)
    }

    /// <summary>
    /// Shared base for all modifier types (entity and character).
    /// Data only — calculation logic lives in ModifierCalculator.
    /// </summary>
    [Serializable]
    public abstract class ModifierBase
    {
        public ModifierType Type;
        public float Value;
        public string Label;

        [NonSerialized]
        private object _source;

        //For auto-setting label when source is an IModifierSource, but can be set manually for other cases or to override.
        public object Source
        {
            get => _source;
            set
            {
                _source = value;
                if (string.IsNullOrEmpty(Label) && value is IModifierSource named)
                    Label = named.ModifierLabel;
            }
        }

        protected ModifierBase(ModifierType type, float value, string label = "")
        {
            Type = type;
            Value = value;
            Label = label;
        }
    }
}

