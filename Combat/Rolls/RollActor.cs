namespace Assets.Scripts.Combat.Rolls
{
    /// <summary>
    /// Sealed hierarchy representing who or what makes a roll.
    /// Eliminates mutually exclusive nullable (Entity, Character) pairs throughout the roll pipeline.
    /// </summary>
    public abstract class RollActor
    {
        public abstract Entity GetEntity();
        public abstract Character GetCharacter();
    }

    /// <summary>A vehicle component making the roll (vehicle checks, vehicle saves).</summary>
    public sealed class ComponentActor : RollActor
    {
        public readonly Entity Component;

        public ComponentActor(Entity component) { Component = component; }

        public override Entity GetEntity() => Component;
        public override Character GetCharacter() => null;
    }

    /// <summary>A character making the roll without a specific tool (character-only checks).</summary>
    public sealed class CharacterActor : RollActor
    {
        public readonly Character Character;

        public CharacterActor(Character character) { Character = character; }

        public override Entity GetEntity() => null;
        public override Character GetCharacter() => Character;
    }

    /// <summary>A character making the roll through a tool component (e.g. gunner via weapon, engineer via power core).</summary>
    public sealed class CharacterWithToolActor : RollActor
    {
        public readonly Character Character;
        public readonly Entity Tool;

        public CharacterWithToolActor(Character character, Entity tool) { Character = character; Tool = tool; }

        public override Entity GetEntity() => Tool;
        public override Character GetCharacter() => Character;
    }
}
