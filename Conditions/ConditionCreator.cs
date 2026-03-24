#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Assets.Scripts.Entities;
using Assets.Scripts.Combat.Damage;
using Assets.Scripts.Combat.Restoration;
using Assets.Scripts.Combat.Rolls.Advantage;
using Assets.Scripts.Conditions.EntityConditions;
using Assets.Scripts.Conditions.CharacterConditions;

namespace Assets.Scripts.Conditions
{
    /// <summary>
    /// Convenience editor class to create new StatusEffect and CharacterCondition assets with preset configurations.
    /// </summary>
    public static class ConditionCreator
    {
        private const string MenuPath = "Assets/Create/Racing/Entity Condition/";
        private const string ConditionMenuPath = "Assets/Create/Racing/Character Condition/";

        [MenuItem(MenuPath + "Buff")]
        public static void CreateBuff()
        {
            CreateAsset("NewBuff", MakeBlankBuff());
        }

        [MenuItem(MenuPath + "Debuff")]
        public static void CreateDebuff()
        {
            CreateAsset("NewDebuff", MakeBlankDebuff());
        }

        [MenuItem(MenuPath + "Damage over Time")]
        public static void CreateDoT()
        {
            CreateAsset("NewDoT", MakeBlankDoT());
        }

        [MenuItem(MenuPath + "Healing over Time")]
        public static void CreateHoT()
        {
            CreateAsset("NewHoT", MakeBlankHoT());
        }

        [MenuItem(ConditionMenuPath + "Character Condition")]
        public static void CreateCharacterCondition()
        {
            CreateAsset("NewCondition", MakeBlankCondition());
        }

        private static void CreateAsset(string defaultName, ScriptableObject definition)
        {
            string path = "Assets";

            if (Selection.activeObject != null)
            {
                path = AssetDatabase.GetAssetPath(Selection.activeObject);
                if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
                    path = System.IO.Path.GetDirectoryName(path);
            }

            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{path}/{defaultName}.asset");

            AssetDatabase.CreateAsset(definition, assetPath);
            AssetDatabase.SaveAssets();

            Selection.activeObject = definition;
            EditorUtility.FocusProjectWindow();
        }

        private static EntityCondition MakeBlankBuff()
            => Make("New Buff", baseDuration: 3,
                modifiers: Mods(Mod(Attribute.Mobility, ModifierType.Flat, 2f)));

        private static EntityCondition MakeBlankDebuff()
            => Make("New Debuff", baseDuration: 3,
                modifiers: Mods(Mod(Attribute.Mobility, ModifierType.Flat, -2f)));

        private static EntityCondition MakeBlankDoT()
            => Make("New DoT", baseDuration: 3,
                periodicEffects: Periodics(
                    DamagePeriodic(new DamageFormula { baseDice = 1, dieSize = 6, bonus = 0, damageType = DamageType.Fire })));

        private static EntityCondition MakeBlankHoT()
            => Make("New HoT", baseDuration: 3,
                periodicEffects: Periodics(RestorationPeriodic(ResourceType.Health, 0, 6, 5)));

        private static CharacterCondition MakeBlankCondition()
            => MakeCondition("New Condition", baseDuration: 2);

        #region named effect catalogue
        // ==================== NAMED EFFECT CATALOGUE ====================
        // Define all named effects here. Run "Assets/Racing/Regenerate All Status Effects" to write them to disk.
        // First run: drag generated assets to skill/card definitions once. Subsequent regenerations overwrite the
        // same .asset file (same GUID) so all references are preserved automatically.

        public const string StatusEffectsFolder = "Assets/Content/StatusEffects";
        public const string ConditionsFolder = "Assets/Content/CharacterConditions";

        [MenuItem("Assets/Racing/Regenerate All Status Effects")]
        public static void RegenerateAllStatusEffects()
        {
            System.IO.Directory.CreateDirectory(StatusEffectsFolder);
            System.IO.Directory.CreateDirectory(ConditionsFolder);

            // Entity status effects
            RegenerateEntityCondition(DefineBurning());
            RegenerateEntityCondition(DefineBleeding());
            RegenerateEntityCondition(DefineOverheating());
            RegenerateEntityCondition(DefineSlowed());
            RegenerateEntityCondition(DefineFortified());
            RegenerateEntityCondition(DefineVulnerable());
            RegenerateEntityCondition(DefineRegenerating());

            // Character conditions
            RegenerateCharacterCondition(DefineStunned());
            RegenerateCharacterCondition(DefineInspired());
            RegenerateCharacterCondition(DefineBlinded());

            AssetDatabase.SaveAssets();
            Debug.Log("[StatusEffectCreator] All status effects and character conditions regenerated.");
        }

        // ---- Entity effect definitions ----

        // Pattern: DoT with feature requirement.
        private static EntityCondition DefineBurning()
            => Make("Burning", baseDuration: 3,
                categories: ConditionCategory.Debuff | ConditionCategory.DoT,
                periodicEffects: Periodics(
                    DamagePeriodic(new DamageFormula { baseDice = 1, dieSize = 6, bonus = 0, damageType = DamageType.Fire })));

        // Pattern: DoT, no feature requirement.
        private static EntityCondition DefineBleeding()
            => Make("Bleeding", baseDuration: 3,
                categories: ConditionCategory.Debuff | ConditionCategory.DoT,
                periodicEffects: Periodics(
                    DamagePeriodic(new DamageFormula { baseDice = 1, dieSize = 4, bonus = 0, damageType = DamageType.Physical })));

        // Pattern: modifier + periodic drain, requires electronic.
        private static EntityCondition DefineOverheating()
            => Make("Overheating", baseDuration: 3,
                categories: ConditionCategory.Debuff | ConditionCategory.DoT,
                required: EntityFeature.IsElectronic,
                modifiers: Mods(
                    Mod(Attribute.MaxSpeed, ModifierType.Flat, -20f),
                    Mod(Attribute.EnergyRegen, ModifierType.Flat, -2f)),
                periodicEffects: Periodics(
                    RestorationPeriodic(ResourceType.Energy, 0, 6, 3, isDrain: true)));

        // Pattern: modifier — reduces speed and mobility.
        private static EntityCondition DefineSlowed()
            => Make("Slowed", baseDuration: 2,
                stackBehaviour: StackBehaviour.Stack,
                maxStacks: 3,
                categories: ConditionCategory.Debuff | ConditionCategory.CrowdControl,
                modifiers: Mods(
                    Mod(Attribute.MaxSpeed, ModifierType.Multiplier, 0.5f),
                    Mod(Attribute.Mobility, ModifierType.Flat, -3f)));

        // Pattern: multi-modifier buff — armor and integrity bonus.
        private static EntityCondition DefineFortified()
            => Make("Fortified", baseDuration: 2,
                stackBehaviour: StackBehaviour.Replace,
                categories: ConditionCategory.Buff | ConditionCategory.AttributeModifier,
                modifiers: Mods(
                    Mod(Attribute.ArmorClass, ModifierType.Flat, 3f),
                    Mod(Attribute.Integrity, ModifierType.Flat, 2f)));

        // Pattern: behavioral — amplifies damage taken.
        private static EntityCondition DefineVulnerable()
            => Make("Vulnerable", baseDuration: 2,
                stackBehaviour: StackBehaviour.Stack,
                maxStacks: 3,
                categories: ConditionCategory.Debuff);

        // Pattern: HoT — periodic healing per turn.
        private static EntityCondition DefineRegenerating()
            => Make("Regenerating", baseDuration: 3,
                categories: ConditionCategory.Buff | ConditionCategory.HoT,
                periodicEffects: Periodics(
                    RestorationPeriodic(ResourceType.Health, 0, 6, 8)));

        // ---- Character condition definitions ----

        // Pattern: behavioral — prevents all actions, short duration.
        private static CharacterCondition DefineStunned()
            => MakeCondition("Stunned", baseDuration: 1,
                stackBehaviour: StackBehaviour.Ignore,
                categories: ConditionCategory.Debuff | ConditionCategory.CrowdControl,
                behavioral: Behavioral(preventsActions: true));

        // Pattern: advantage grant on character checks.
        private static CharacterCondition DefineInspired()
            => MakeCondition("Inspired", baseDuration: 2,
                categories: ConditionCategory.Buff,
                removalTriggers: RemovalTrigger.OnD20Roll,
                advantageGrants: new List<AdvantageGrant>
                {
                    AdvGrant("Inspired", RollMode.Advantage, new CharacterCheckAdvantage())
                });

        // Pattern: disadvantage grant on attacks.
        private static CharacterCondition DefineBlinded()
            => MakeCondition("Blinded", baseDuration: 2,
                stackBehaviour: StackBehaviour.Ignore,
                categories: ConditionCategory.Debuff | ConditionCategory.CrowdControl,
                advantageGrants: new List<AdvantageGrant>
                {
                    AdvGrant("Blinded", RollMode.Disadvantage, new AttackAdvantage())
                });

        private static void RegenerateEntityCondition(EntityCondition definition)
        {
            string path = $"{StatusEffectsFolder}/{definition.name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<EntityCondition>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(definition, existing);
                EditorUtility.SetDirty(existing);
            }
            else
            {
                AssetDatabase.CreateAsset(definition, path);
            }
        }

        private static void RegenerateCharacterCondition(CharacterCondition definition)
        {
            string path = $"{ConditionsFolder}/{definition.name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<CharacterCondition>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(definition, existing);
                EditorUtility.SetDirty(existing);
            }
            else
            {
                AssetDatabase.CreateAsset(definition, path);
            }
        }

        // ==================== BUILDER METHODS ====================

        private static EntityModifierData Mod(Attribute attribute, ModifierType type, float value)
            => new EntityModifierData { attribute = attribute, type = type, value = value };

        private static List<EntityModifierData> Mods(params EntityModifierData[] mods)
            => new List<EntityModifierData>(mods);

        private static IPeriodicEffect DamagePeriodic(DamageFormula formula)
            => new PeriodicDamageEffect { damageFormula = formula };

        private static IPeriodicEffect RestorationPeriodic(ResourceType resourceType, int baseDice, int dieSize, int bonus, bool isDrain = false)
            => new PeriodicRestorationEffect
            {
                formula = new RestorationFormula
                {
                    resourceType = resourceType,
                    isDrain = isDrain,
                    baseDice = baseDice,
                    dieSize = dieSize,
                    bonus = bonus
                }
            };

        private static List<IPeriodicEffect> Periodics(params IPeriodicEffect[] effects)
            => new List<IPeriodicEffect>(effects);

        private static BehavioralEffectData Behavioral(
            bool preventsActions = false,
            bool preventsMovement = false)
            => new BehavioralEffectData
            {
                preventsActions = preventsActions,
                preventsMovement = preventsMovement
            };

        private static AdvantageGrant AdvGrant(string label, RollMode type, params IAdvantageTarget[] targets)
            => new AdvantageGrant
            {
                label = label,
                type = type,
                targets = new List<IAdvantageTarget>(targets)
            };

        private static EntityCondition Make(
            string name,
            int baseDuration = -1,
            StackBehaviour stackBehaviour = StackBehaviour.Refresh,
            int maxStacks = 0,
            ConditionCategory categories = ConditionCategory.None,
            RemovalTrigger removalTriggers = RemovalTrigger.None,
            EntityFeature required = EntityFeature.None,
            EntityFeature excluded = EntityFeature.None,
            List<EntityModifierData> modifiers = null,
            List<IPeriodicEffect> periodicEffects = null,
            BehavioralEffectData behavioral = null,
            List<AdvantageGrant> advantageGrants = null)
        {
            var effect = ScriptableObject.CreateInstance<EntityCondition>();
            effect.name = name;
            effect.effectName = name;
            effect.baseDuration = baseDuration;
            effect.stackBehaviour = stackBehaviour;
            effect.maxStacks = maxStacks;
            effect.categories = categories;
            effect.removalTriggers = removalTriggers;
            effect.requiredFeatures = required;
            effect.excludedFeatures = excluded;
            effect.modifiers = modifiers ?? new List<EntityModifierData>();
            effect.periodicEffects = periodicEffects ?? new List<IPeriodicEffect>();
            effect.behavioralEffects = behavioral ?? new BehavioralEffectData();
            effect.advantageGrants = advantageGrants ?? new List<AdvantageGrant>();
            return effect;
        }

        private static CharacterCondition MakeCondition(
            string name,
            int baseDuration = -1,
            StackBehaviour stackBehaviour = StackBehaviour.Refresh,
            int maxStacks = 0,
            ConditionCategory categories = ConditionCategory.None,
            RemovalTrigger removalTriggers = RemovalTrigger.None,
            List<CharacterModifierData> modifiers = null,
            BehavioralEffectData behavioral = null,
            List<AdvantageGrant> advantageGrants = null)
        {
            var condition = ScriptableObject.CreateInstance<CharacterCondition>();
            condition.name = name;
            condition.effectName = name;
            condition.baseDuration = baseDuration;
            condition.stackBehaviour = stackBehaviour;
            condition.maxStacks = maxStacks;
            condition.categories = categories;
            condition.removalTriggers = removalTriggers;
            condition.modifiers = modifiers ?? new List<CharacterModifierData>();
            condition.behavioralEffects = behavioral ?? new BehavioralEffectData();
            condition.advantageGrants = advantageGrants ?? new List<AdvantageGrant>();
            return condition;
        }
        #endregion
    }
}
#endif
