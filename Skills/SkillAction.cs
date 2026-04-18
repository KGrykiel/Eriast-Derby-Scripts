using Assets.Scripts.Combat.Rolls;
using Assets.Scripts.Entities;

namespace Assets.Scripts.Skills
{
    /// <summary>
    /// A fully-resolved, ready-to-execute action. Carries everything the execution
    /// pipeline needs: which skill to fire, who is acting, and what the target is.
    /// Constructed by both the player input system and the AI selection stage.
    /// </summary>
    public class SkillAction
    {
        public readonly Skill skill;
        public readonly RollActor sourceActor;
        public readonly IRollTarget target;

        public SkillAction(Skill skill, RollActor sourceActor, IRollTarget target)
        {
            this.skill = skill;
            this.sourceActor = sourceActor;
            this.target = target;
        }
    }
}
