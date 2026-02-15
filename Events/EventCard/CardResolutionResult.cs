namespace Assets.Scripts.Events.EventCard
{
    public class CardResolutionResult
    {
        public bool success;
        public string narrativeOutcome;

        public CardResolutionResult(bool success, string narrative)
        {
            this.success = success;
            this.narrativeOutcome = narrative;
        }
    }
}
