#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Assets.Scripts.Combat.Damage;
using System.Collections.Generic;
using Assets.Scripts.Entities.Vehicle;
using Assets.Scripts.Combat.SkillChecks;
using Assets.Scripts.Combat.Saves;

namespace Assets.Scripts.Skills
{
    /// <summary>
    /// Convenience editor class to create new Skill assets with preset configurations based on category.
    /// </summary>
    public static class SkillCreator
    {
        private const string MenuPath = "Assets/Create/Racing/Skill/";

        [MenuItem(MenuPath + "Attack Skill")]
        public static void CreateAttackSkill()
        {
            CreateSkillAsset(SkillCategory.Attack, "NewAttackSkill");
        }

        [MenuItem(MenuPath + "Restoration Skill")]
        public static void CreateRestorationSkill()
        {
            CreateSkillAsset(SkillCategory.Restoration, "NewRestorationSkill");
        }

        [MenuItem(MenuPath + "Buff Skill")]
        public static void CreateBuffSkill()
        {
            CreateSkillAsset(SkillCategory.Buff, "NewBuffSkill");
        }

        [MenuItem(MenuPath + "Debuff Skill")]
        public static void CreateDebuffSkill()
        {
            CreateSkillAsset(SkillCategory.Debuff, "NewDebuffSkill");
        }

        [MenuItem(MenuPath + "Utility Skill")]
        public static void CreateUtilitySkill()
        {
            CreateSkillAsset(SkillCategory.Utility, "NewUtilitySkill");
        }

        [MenuItem(MenuPath + "Special Skill")]
        public static void CreateSpecialSkill()
        {
            CreateSkillAsset(SkillCategory.Special, "NewSpecialSkill");
        }

        [MenuItem(MenuPath + "Custom Skill")]
        public static void CreateCustomSkill()
        {
            CreateSkillAsset(SkillCategory.Custom, "NewCustomSkill");
        }

        private static void CreateSkillAsset(SkillCategory category, string defaultName)
        {
            Skill skill = ScriptableObject.CreateInstance<Skill>();
            skill.category = category;
            ApplyPresets(skill);

            string path = "Assets";

            if (Selection.activeObject != null)
            {
                path = AssetDatabase.GetAssetPath(Selection.activeObject);
                if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
                    path = System.IO.Path.GetDirectoryName(path);
            }

            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{path}/{defaultName}.asset");

            AssetDatabase.CreateAsset(skill, assetPath);
            AssetDatabase.SaveAssets();

            Selection.activeObject = skill;
            EditorUtility.FocusProjectWindow();
        }
        
        private static void ApplyPresets(Skill skill)
        {
            skill.effectInvocations = skill.category switch
            {
                SkillCategory.Attack => GetAttackPreset(),
                SkillCategory.Restoration => GetRestorationPreset(),
                SkillCategory.Buff => GetBuffPreset(),
                SkillCategory.Debuff => GetDebuffPreset(),
                SkillCategory.Special => GetSpecialPreset(),
                _ => new List<EffectInvocation>()
            };

            switch (skill.category)
            {
                case SkillCategory.Attack:
                    skill.skillRollType = SkillRollType.AttackRoll;
                    skill.saveSpec = SaveSpec.None;
                    skill.saveDCBase = 0;
                    skill.checkSpec = SkillCheckSpec.None;
                    skill.checkDC = 0;
                    skill.targetingMode = TargetingMode.Enemy;
                    break;
                    
                case SkillCategory.Restoration:
                    skill.skillRollType = SkillRollType.None;
                    skill.saveSpec = SaveSpec.None;
                    skill.saveDCBase = 0;
                    skill.checkSpec = SkillCheckSpec.None;
                    skill.checkDC = 0;
                    skill.targetingMode = TargetingMode.SourceComponent;
                    break;
                    
                case SkillCategory.Buff:
                    skill.skillRollType = SkillRollType.SkillCheck;
                    skill.saveSpec = SaveSpec.None;
                    skill.saveDCBase = 0;
                    skill.checkSpec = SkillCheckSpec.ForVehicle(VehicleCheckAttribute.Mobility);
                    skill.checkDC = 15;
                    skill.targetingMode = TargetingMode.Self;
                    break;
                    
                case SkillCategory.Debuff:
                    skill.skillRollType = SkillRollType.SavingThrow;
                    skill.saveSpec = SaveSpec.ForVehicle(VehicleCheckAttribute.Mobility);
                    skill.saveDCBase = 15;
                    skill.checkSpec = SkillCheckSpec.None;
                    skill.checkDC = 0;
                    skill.targetingMode = TargetingMode.Enemy;
                    break;
                    
                case SkillCategory.Utility:
                    skill.skillRollType = SkillRollType.None;
                    skill.saveSpec = SaveSpec.None;
                    skill.saveDCBase = 0;
                    skill.checkSpec = SkillCheckSpec.None;
                    skill.checkDC = 0;
                    skill.targetingMode = TargetingMode.Self;
                    break;
                    
                case SkillCategory.Special:
                    skill.skillRollType = SkillRollType.None;
                    skill.saveSpec = SaveSpec.None;
                    skill.saveDCBase = 0;
                    skill.checkSpec = SkillCheckSpec.None;
                    skill.checkDC = 0;
                    skill.targetingMode = TargetingMode.Enemy;
                    break;
                    
                case SkillCategory.Custom:
                    break;
            }
        }
        
        // ==================== PRESET GENERATORS ====================
        
        private static List<EffectInvocation> GetAttackPreset()
        {
            return new List<EffectInvocation>
            {
                new() {
                    effect = new DamageEffect
                    {
                        formulaProvider = new StaticFormulaProvider
                        {
                            formula = new DamageFormula
                            {
                                baseDice = 1,
                                dieSize = 6,
                                bonus = 0,
                                damageType = DamageType.Physical
                            }
                        }
                    },
                    target = EffectTarget.SelectedTarget
                }
            };
        }
        
        private static List<EffectInvocation> GetRestorationPreset()
        {
            return new List<EffectInvocation>
            {
                new() {
                    effect = new ResourceRestorationEffect(),
                    target = EffectTarget.SourceVehicle
                }
            };
        }
        
        private static List<EffectInvocation> GetBuffPreset()
        {
            return new List<EffectInvocation>
            {
                new() {
                    effect = new ApplyStatusEffect(),
                    target = EffectTarget.SourceVehicle
                }
            };
        }

        private static List<EffectInvocation> GetDebuffPreset()
        {
            return new List<EffectInvocation>
            {
                new() {
                    effect = new ApplyStatusEffect(),
                    target = EffectTarget.SelectedTarget
                }
            };
        }

        private static List<EffectInvocation> GetSpecialPreset()
        {
            return new List<EffectInvocation>
            {
                new() {
                    effect = new CustomEffect(),
                    target = EffectTarget.SelectedTarget
                }
            };
        }
    }
}
#endif
