namespace Assets.Scripts.AI.Execution
{
    /// <summary>
    /// Execution module. Receives an <see cref="AIAction"/> and fires it through the
    /// existing game systems. Executors never perform scoring or perception — only
    /// state mutation.
    /// </summary>
    public interface IExecutor
    {
        void Execute(AIAction action, TurnService turnService);
    }
}
