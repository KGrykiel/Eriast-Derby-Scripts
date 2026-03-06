#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Assets.Scripts.Combat.Damage;
using System.Collections.Generic;
using Assets.Scripts.Entities.Vehicle;
using Assets.Scripts.Combat.RollSpecs;

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
            var successEffects = skill.category switch
            {
                SkillCategory.Attack     => GetAttackPreset(),
                SkillCategory.Restoration => GetRestorationPreset(),
                SkillCategory.Buff       => GetBuffPreset(),
                SkillCategory.Debuff     => GetDebuffPreset(),
                SkillCategory.Special    => GetSpecialPreset(),
                _                        => new List<EffectInvocation>()
            };

            skill.rollNode = skill.category switch
            {
                SkillCategory.Attack => new RollNode
                {
                    rollSpec = new AttackSpec(),
                    successEffects = successEffects
                },
                SkillCategory.Buff => new RollNode
                {
                    rollSpec = SkillCheckSpec.ForVehicle(VehicleCheckAttribute.Mobility),
                    dc = 15,
                    successEffects = successEffects
                },
                SkillCategory.Debuff => new RollNode
                {
                    rollSpec = SaveSpec.ForVehicle(VehicleCheckAttribute.Mobility),
                    dc = 15,
                    failureEffects = successEffects  // debuff applies when target fails the save
                },
                _ => new RollNode { successEffects = successEffects }
            };

            skill.targetingMode = skill.category switch
            {
                SkillCategory.Attack  => TargetingMode.Enemy,
                SkillCategory.Debuff  => TargetingMode.Enemy,
                SkillCategory.Buff    => TargetingMode.Self,
                SkillCategory.Restoration => TargetingMode.SourceComponent,
                _                     => TargetingMode.Self
            };
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
