using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Characters;

/// <summary>
/// Represents a character that can operate vehicle components.
/// This is a data class — characters store attributes, proficiencies, and identity.
/// All bonus calculations are performed by the calculators (SkillCheckCalculator, SaveCalculator).
/// 
/// Characters work through vehicle components. Their seat determines which components
/// they can use. CheckResolver resolves the character + component pairing.
/// </summary>
[CreateAssetMenu(fileName = "New Character", menuName = "Racing/Player Character")]
public class PlayerCharacter : ScriptableObject
{
    [Header("Character Identity")]
    [Tooltip("Character's display name")]
    public string characterName = "Unnamed Character";
    
    [TextArea(2, 4)]
    [Tooltip("Character description/background")]
    public string description = "";

    [Header("Level")]
    [Tooltip("Character level. Proficiency bonus equals level.")]
    [Min(1)]
    public int level = 1;
    
    // ==================== ATTRIBUTES ====================
    
    [Header("Attributes (3-20, default 10)")]
    [Tooltip("Raw physical power, lifting, breaking. May govern future melee/grapple skills.")]
    [SerializeField, Range(3, 20)]
    private int strength = 10;
    
    [Tooltip("Reflexes, hand-eye coordination, fine motor control. Governs: Piloting, Defensive Maneuvers, Stunts, Stealth.")]
    [SerializeField, Range(3, 20)]
    private int dexterity = 10;
    
    [Tooltip("Technical knowledge, problem-solving, understanding of magical systems. Governs: Mechanics, Arcana.")]
    [SerializeField, Range(3, 20)]
    private int intelligence = 10;
    
    [Tooltip("Awareness, intuition, reading situations. Governs: Perception, Survival.")]
    [SerializeField, Range(3, 20)]
    private int wisdom = 10;
    
    [Tooltip("Endurance, withstanding physical stress. Used for CON saves.")]
    [SerializeField, Range(3, 20)]
    private int constitution = 10;
    
    [Tooltip("Showmanship, force of personality, intimidation. Governs: Deception, Intimidation.")]
    [SerializeField, Range(3, 20)]
    private int charisma = 10;
    
    // ==================== PROFICIENCIES ====================
    
    [Header("Skill Proficiencies")]
    [Tooltip("Skills this character is proficient in. Grants bonus equal to level.")]
    [SerializeField]
    private List<CharacterSkill> proficientSkills = new();

    // ==================== COMBAT STATS ====================
    
    [Header("Combat Stats")]
    [Tooltip("Base attack bonus for weapon attacks.")]
    public int baseAttackBonus = 0;
    
    [Header("Legacy Skills")]
    [Tooltip("Personal skills this character brings (independent of component). Existing skill system.")]
    public List<Skill> personalSkills = new();

    // ==================== ATTRIBUTE METHODS ====================
    
    /// <summary>
    /// Get the raw score for a character attribute.
    /// </summary>
    public int GetAttributeScore(CharacterAttribute attribute)
    {
        return attribute switch
        {
            CharacterAttribute.Strength => strength,
            CharacterAttribute.Dexterity => dexterity,
            CharacterAttribute.Intelligence => intelligence,
            CharacterAttribute.Wisdom => wisdom,
            CharacterAttribute.Constitution => constitution,
            CharacterAttribute.Charisma => charisma,
            _ => 10
        };
    }
    
    /// <summary>
    /// Get the modifier for a character attribute.
    /// Standard D&D formula: (score - 10) / 2, rounded down.
    /// </summary>
    public int GetAttributeModifier(CharacterAttribute attribute)
    {
        int score = GetAttributeScore(attribute);
        return (score - 10) / 2;
    }
    
    // ==================== PROFICIENCY METHODS ====================
    
    /// <summary>
    /// Check if this character is proficient in a skill.
    /// </summary>
    public bool IsProficient(CharacterSkill skill)
    {
        return proficientSkills != null && proficientSkills.Contains(skill);
    }
    
    /// <summary>
    /// Get the proficiency bonus for a skill.
    /// Returns level if proficient, 0 otherwise.
    /// </summary>
    public int GetProficiencyBonus(CharacterSkill skill)
    {
        return IsProficient(skill) ? level : 0;
    }
    
    // ==================== LEGACY METHODS ====================
    
    /// <summary>
    /// Get personal skills this character contributes to their component.
    /// These skills are available regardless of which component they're assigned to.
    /// </summary>
    public List<Skill> GetPersonalSkills()
    {
        return personalSkills != null ? new List<Skill>(personalSkills) : new List<Skill>();
    }
    
    /// <summary>
    /// Get a display-friendly summary of this character.
    /// </summary>
    public string GetCharacterSummary()
    {
        string summary = $"<b>{characterName}</b> (Level {level})\n";
        
        if (!string.IsNullOrEmpty(description))
        {
            summary += $"{description}\n\n";
        }
        
        summary += $"STR {strength} ({GetAttributeModifier(CharacterAttribute.Strength):+#;-#;0}) | ";
        summary += $"DEX {dexterity} ({GetAttributeModifier(CharacterAttribute.Dexterity):+#;-#;0}) | ";
        summary += $"INT {intelligence} ({GetAttributeModifier(CharacterAttribute.Intelligence):+#;-#;0}) | ";
        summary += $"WIS {wisdom} ({GetAttributeModifier(CharacterAttribute.Wisdom):+#;-#;0}) | ";
        summary += $"CON {constitution} ({GetAttributeModifier(CharacterAttribute.Constitution):+#;-#;0}) | ";
        summary += $"CHA {charisma} ({GetAttributeModifier(CharacterAttribute.Charisma):+#;-#;0})\n\n";
        
        if (proficientSkills.Count > 0)
        {
            summary += "Proficiencies: ";
            for (int i = 0; i < proficientSkills.Count; i++)
            {
                if (i > 0) summary += ", ";
                summary += proficientSkills[i].ToString();
            }
            summary += $" (+{level})\n";
        }
        
        return summary;
    }
}
