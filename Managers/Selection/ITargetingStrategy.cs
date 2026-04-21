namespace Assets.Scripts.Managers.Selection
{
    /// <summary>
    /// Owns the complete targeting flow for one <see cref="Skills.TargetingMode"/>.
    /// Invokes the completion callback with the resolved target when done.
    /// </summary>
    public interface ITargetingStrategy
    {
        void Execute(SelectionContext ctx);
    }
}
