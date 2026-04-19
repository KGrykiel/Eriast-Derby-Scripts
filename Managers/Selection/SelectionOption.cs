namespace Assets.Scripts.Managers.Selection
{
    /// <summary>
    /// A single displayable choice in a selection UI panel.
    /// </summary>
    public class SelectionOption<T>
    {
        public T Value { get; }
        public string Label { get; }
        public bool Interactable { get; }

        public SelectionOption(T value, string label, bool interactable = true)
        {
            Value = value;
            Label = label;
            Interactable = interactable;
        }
    }
}
