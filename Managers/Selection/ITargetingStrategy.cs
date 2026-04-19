namespace Assets.Scripts.Managers.Selection
{
    /// <summary>
    /// Owns the complete targeting flow for one <see cref="Skills.TargetingMode"/>.
    /// Invokes <see cref="SelectionContext.OnComplete"/> with the resolved target when done.
    /// </summary>
    public interface ITargetingStrategy
    {
        void Execute(SelectionContext ctx);
    }
}
