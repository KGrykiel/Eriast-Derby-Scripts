namespace Assets.Scripts.Characters
{
    public abstract class CharacterModifier : ModifierBase
    {
        protected CharacterModifier(ModifierType type, float value, string label)
            : base(type, value, label)
        {
        }
    }

    public class CharacterSkillModifier : CharacterModifier
    {
        public CharacterSkill Skill;

        public CharacterSkillModifier(CharacterSkill skill, ModifierType type, float value, string label)
            : base(type, value, label)
        {
            Skill = skill;
        }
    }

    public class CharacterAttributeModifier : CharacterModifier
    {
        public CharacterAttribute Attribute;

        public CharacterAttributeModifier(CharacterAttribute attribute, ModifierType type, float value, string label)
            : base(type, value, label)
        {
            Attribute = attribute;
        }
    }
}