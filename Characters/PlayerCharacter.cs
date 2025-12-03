using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PlayerCharacter stub - represents a character that can operate vehicle components.
/// TODO: Expand this when character classes and progression are designed.
/// This is intentionally minimal to allow maximum flexibility for future design.
/// </summary>
[CreateAssetMenu(fileName = "New Character", menuName = "Racing Game/Player Character")]
public class PlayerCharacter : ScriptableObject
{
    [Header("Character Identity")]
    [Tooltip("Character's display name")]
    public string characterName = "Unnamed Character";
    
    [TextArea(2, 4)]
    [Tooltip("Character description/background")]
    public string description = "";
    
    // TODO: Add character class system when designed
    // This might be an enum, a string, or a reference to a CharacterClass ScriptableObject
    // Example future fields:
    // public CharacterClass characterClass;
    // public int level = 1;
    
    [Header("Character Skills")]
    [Tooltip("Personal skills this character brings (independent of component)")]
    public List<Skill> personalSkills = new List<Skill>();
    
    // TODO: Add character stats when system is designed
    // These might be D&D stats, custom racing stats, or something else entirely
    // Example future fields:
    // public int strength = 10;
    // public int dexterity = 10;
    // public int engineeringSkill = 0;
    // public int shootingAccuracy = 0;
    // public Dictionary<string, int> customStats;
    
    // TODO: Add character progression when designed
    // Example future fields:
    // public int experience = 0;
    // public List<CharacterAbility> unlockedAbilities;
    // public List<CharacterPerk> perks;
    
    /// <summary>
    /// Get personal skills this character contributes to their component.
    /// These skills are available regardless of which component they're assigned to.
    /// </summary>
    public List<Skill> GetPersonalSkills()
    {
        return personalSkills != null ? new List<Skill>(personalSkills) : new List<Skill>();
    }
    
    // TODO: Add stat modifier methods when stat system is designed
    // Example future methods:
    // public int GetModifier(string statName) { ... }
    // public int GetBonusForSkill(Skill skill) { ... }
    // public float GetComponentEfficiencyModifier() { ... }
    
    /// <summary>
    /// Get a display-friendly summary of this character.
    /// </summary>
    public string GetCharacterSummary()
    {
        string summary = $"<b>{characterName}</b>\n";
        
        if (!string.IsNullOrEmpty(description))
        {
            summary += $"{description}\n\n";
        }
        
        if (personalSkills.Count > 0)
        {
            summary += $"Personal Skills: {personalSkills.Count}\n";
            foreach (var skill in personalSkills)
            {
                if (skill != null)
                    summary += $"  - {skill.name}\n";
            }
        }
        
        // TODO: Add class, level, stats when designed
        
        return summary;
    }
}
