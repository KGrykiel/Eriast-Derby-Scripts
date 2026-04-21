using Assets.Scripts.Skills;

namespace Assets.Scripts.Managers
{
    /// <summary>
    /// This is the future home of stepped playback, inter-action delays,
    /// and attack line rendering.
    /// </summary>
    public class VehicleActionManager
    {
        public void Submit(SkillAction action)
        {
            // Future: visual effects, stepped playback pause, inter-action delay here.
            SkillPipeline.Execute(action);
        }
    }
}
