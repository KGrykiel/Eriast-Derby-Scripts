using UnityEngine;
using Assets.Scripts.Characters;

namespace Assets.Scripts.Tests.Helpers
{
    public static class TestCharacterFactory
    {
        /// <summary>
        /// Create a character for testing (without cleanup tracking).
        /// </summary>
        public static Character Create(
            string name = "TestChar",
            int level = 1,
            int dexterity = 10,
            int wisdom = 10,
            int intelligence = 10,
            int constitution = 10,
            int strength = 10,
            int charisma = 10,
            int baseAttackBonus = 0)
        {
            var character = ScriptableObject.CreateInstance<Character>();
            character.characterName = name;
            character.level = level;
            character.baseAttackBonus = baseAttackBonus;

            // Use reflection to set private serialized fields
            SetPrivateField(character, "dexterity", dexterity);
            SetPrivateField(character, "wisdom", wisdom);
            SetPrivateField(character, "intelligence", intelligence);
            SetPrivateField(character, "constitution", constitution);
            SetPrivateField(character, "strength", strength);
            SetPrivateField(character, "charisma", charisma);

            return character;
        }

        /// <summary>
        /// Create a character and automatically track it for cleanup.
        /// Use this in test methods that need automatic disposal.
        /// </summary>
        public static Character CreateWithCleanup(
            string name = "TestChar",
            int level = 1,
            int dexterity = 10,
            int wisdom = 10,
            int intelligence = 10,
            int constitution = 10,
            int strength = 10,
            int charisma = 10,
            int baseAttackBonus = 0,
            System.Collections.Generic.List<Object> cleanup = null)
        {
            var character = Create(name, level, dexterity, wisdom, intelligence, constitution, strength, charisma, baseAttackBonus);
            cleanup?.Add(character);
            return character;
        }

        public static void AddProficiency(Character character, CharacterSkill skill)
        {
            var field = typeof(Character).GetField("proficientSkills",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var list = (System.Collections.Generic.List<CharacterSkill>)field.GetValue(character);
            list.Add(skill);
        }

        private static void SetPrivateField(Character character, string fieldName, int value)
        {
            var field = typeof(Character).GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(character, value);
        }
    }
}
