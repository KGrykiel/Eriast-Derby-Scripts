using Assets.Scripts.Combat.Rolls;
using Assets.Scripts.Entities;
using Assets.Scripts.Skills;

namespace Assets.Scripts.AI
{
    /// <summary>
    /// Internal hand-off from Selection to Execution inside a single <see cref="SeatAI"/>
    /// turn. Never leaves the seat. Fields are readonly: an AIAction is immutable
    /// once chosen. No nullable fields — the seat either produces a complete action
    /// or produces none at all.
    /// </summary>
    public class AIAction
    {
        public readonly Skill skill;
        public readonly IRollTarget target;
        public readonly RollActor sourceActor;
        public readonly float score;

        public AIAction(Skill skill, IRollTarget target, RollActor sourceActor, float score)
        {
            this.skill = skill;
            this.target = target;
            this.sourceActor = sourceActor;
            this.score = score;
        }
    }
}
