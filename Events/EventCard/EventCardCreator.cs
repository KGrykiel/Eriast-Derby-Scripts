#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Assets.Scripts.Events.EventCard.EventCardTypes;
using Assets.Scripts.Combat.Rolls.RollSpecs;
using Assets.Scripts.Combat.Rolls.RollSpecs.SpecTypes;
using Assets.Scripts.Entities.Vehicle;
using Assets.Scripts.Characters;
using Assets.Scripts.Combat.Damage;
using StatusEffectTemplate = Assets.Scripts.StatusEffects.StatusEffect;

namespace Assets.Scripts.Events.EventCard
{
    /// <summary>
    /// Convenience editor class to create new EventCard assets with preset configurations based on type.
    /// </summary>
    public static class EventCardCreator
    {
        private const string MenuPath = "Assets/Create/Racing/Event Card/";

        [MenuItem(MenuPath + "Hazard Card")]
        public static void CreateHazardCard()
        {
            CreateCardAsset("NewHazardCard", MakeBlankHazardCard());
        }

        [MenuItem(MenuPath + "Choice Card")]
        public static void CreateChoiceCard()
        {
            CreateCardAsset("NewChoiceCard", MakeBlankChoiceCard());
        }

        [MenuItem(MenuPath + "Narrative Card")]
        public static void CreateNarrativeCard()
        {
            CreateCardAsset("NewNarrativeCard", MakeBlankNarrativeCard());
        }

        private static void CreateCardAsset(string defaultName, ChoiceCard definition)
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

        private static ChoiceCard MakeBlankHazardCard()
            => Make("New Hazard",
                "A hazard blocks the track!",
                Choice("Press on",
                    Save(SaveSpec.ForVehicle(VehicleCheckAttribute.Stability), 13,
                        FX(Dmg(1, 6, 0, DamageType.Bludgeoning)),
                        successNarrative: "You push through unscathed.",
                        failureNarrative: "The impact rattles your vehicle.")));

        private static ChoiceCard MakeBlankChoiceCard()
            => Make("New Choice",
                "A decision looms ahead.",
                Choice("Take the risk",
                    Check(SkillCheckSpec.ForVehicle(VehicleCheckAttribute.Mobility), 13,
                        FX(),
                        successNarrative: "You navigate it perfectly.",
                        failureNarrative: "You barely manage.")),
                Choice("Play it safe",
                    AlwaysApply(FX(), "You take the cautious route.")));

        private static ChoiceCard MakeBlankNarrativeCard()
            => Make("New Narrative",
                "Something happens on the track.",
                Choice("Continue",
                    AlwaysApply(FX(), "The moment passes.")));

        #region named card catalogue
        // ==================== NAMED CARD CATALOGUE ====================
        // Define all named cards here. Run "Assets/Racing/Regenerate All Event Cards" to write them to disk.
        // First run: drag generated assets to the stage deck once. Subsequent regenerations overwrite the
        // same .asset file (same GUID) so all deck references are preserved automatically.

        private const string CardsFolder = "Assets/Content/EventCards";

        [MenuItem("Assets/Racing/Regenerate All Event Cards")]
        public static void RegenerateAllEventCards()
        {
            System.IO.Directory.CreateDirectory(CardsFolder);

            RegenerateCard(DefineRockfall());
            RegenerateCard(DefineOilSlick());
            RegenerateCard(DefineHiddenShortcut());
            RegenerateCard(DefineFreakStorm());
            RegenerateCard(DefineGustOfWind());
            RegenerateCard(DefineScavengerFind());

            AssetDatabase.SaveAssets();
            Debug.Log("[EventCardCreator] All event cards regenerated.");
        }

        // ---- Card definitions ----

        // Pattern: single-choice hazard — vehicle save or take damage.
        private static ChoiceCard DefineRockfall()
            => Make("Rockfall",
                "Boulders crash down across the track!",
                Choice("Dodge the rocks",
                    Save(SaveSpec.ForVehicle(VehicleCheckAttribute.Mobility), 14,
                        FX(Dmg(2, 6, 0, DamageType.Bludgeoning)),
                        successNarrative: "You weave through the debris unscathed.",
                        failureNarrative: "Rocks slam into your vehicle.")));

        // Pattern: no roll — unconditional effect applied regardless of choice.
        private static ChoiceCard DefineOilSlick()
            => Make("Oil Slick",
                "The road ahead is slicked with oil.",
                Choice("Power through",
                    AlwaysApply(
                        FX(Dmg(1, 4, 0, DamageType.Physical)),
                        "Your tyres screech as you slide through the slick.")));

        // Pattern: 2-choice — character check gates a bonus; safe fallback costs nothing.
        private static ChoiceCard DefineHiddenShortcut()
            => Make("Hidden Shortcut",
                "Your navigator spots what might be a shortcut through the ruins.",
                Choice("Scout the shortcut",
                    Check(SkillCheckSpec.ForCharacter(CharacterSkill.Perception, ComponentType.Sensors), 13,
                        FX(Energy(3)),
                        successNarrative: "The shortcut shaves precious time — you surge ahead.",
                        failureNarrative: "The path dead-ends. You rejoin the main track having lost nothing but time.")),
                Choice("Stay on track",
                    AlwaysApply(FX(), "You stick to the known route.")));

        // Pattern: 2-choice — character save; one option risks damage, the other costs momentum.
        private static ChoiceCard DefineFreakStorm()
            => Make("Freak Storm",
                "A sudden squall hammers the course. Batten down or push through?",
                Choice("Shelter the crew",
                    Save(SaveSpec.ForCharacter(CharacterAttribute.Constitution), 13,
                        FX(Dmg(1, 6, 0, DamageType.Physical)),
                        successNarrative: "The crew braces tight — minimal damage.",
                        failureNarrative: "The storm batters the hull.")),
                Choice("Full throttle",
                    AlwaysApply(
                        FX(Dmg(1, 4, 0, DamageType.Physical)),
                        "You punch through the squall at cost.")));

        // Pattern: chained saves — first vehicle save, on failure a second vehicle save triggers.
        private static ChoiceCard DefineGustOfWind()
            => Make("Gust of Wind",
                "A violent crosswind sweeps across the course.",
                Choice("Hold your line",
                    Save(SaveSpec.ForVehicle(VehicleCheckAttribute.Stability), 12,
                        FX(),
                        successNarrative: "You ride the gust with steady nerves.",
                        failureNarrative: "The wind shunts you sideways.",
                        failChain: Save(SaveSpec.ForVehicle(VehicleCheckAttribute.Mobility), 14,
                            FX(Dmg(2, 4, 0, DamageType.Force)),
                            successNarrative: "You correct before hitting anything.",
                            failureNarrative: "You clip a barrier hard."))));

        // Pattern: check gates a successChain — pass the check to unlock the bonus effect node.
        private static ChoiceCard DefineScavengerFind()
            => Make("Scavenger Find",
                "A crate of salvage sits at the roadside — worth stopping for?",
                Choice("Grab it",
                    Check(SkillCheckSpec.ForCharacter(CharacterSkill.Mechanics), 11,
                        FX(),
                        successNarrative: "You snag the crate without breaking stride.",
                        failureNarrative: "You fumble it — the crate tumbles away.",
                        successChain: AlwaysApply(FX(Energy(4)), "The salvage yields useful components."))),
                Choice("Leave it",
                    AlwaysApply(FX(), "You keep your eyes on the race.")));

        private static void RegenerateCard(ChoiceCard definition)
        {
            string path = $"{CardsFolder}/{definition.name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<ChoiceCard>(path);
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

        /// <summary>Load a StatusEffect asset by project path for use in card definitions.</summary>
        private static StatusEffectTemplate LoadStatus(string assetPath)
            => AssetDatabase.LoadAssetAtPath<StatusEffectTemplate>(assetPath);

        // ==================== BUILDER METHODS ====================

        private static List<EffectInvocation> FX(params EffectInvocation[] effects)
            => new List<EffectInvocation>(effects);

        private static EffectInvocation Dmg(
            int dice, int dieSize, int bonus = 0,
            DamageType type = DamageType.Physical,
            EffectTarget target = EffectTarget.SourceVehicle)
            => new EffectInvocation
            {
                target = target,
                effect = new DamageEffect
                {
                    formulaProvider = new StaticFormulaProvider
                    {
                        formula = new DamageFormula { baseDice = dice, dieSize = dieSize, bonus = bonus, damageType = type }
                    }
                }
            };

        private static EffectInvocation Energy(int amount, EffectTarget target = EffectTarget.SourceVehicle)
            => new EffectInvocation
            {
                target = target,
                effect = new ResourceRestorationEffect { resourceType = ResourceRestorationEffect.ResourceType.Energy, amount = amount }
            };

        private static EffectInvocation Status(StatusEffectTemplate effect, EffectTarget target = EffectTarget.SourceVehicle)
            => new EffectInvocation
            {
                target = target,
                effect = new ApplyStatusEffect { statusEffect = effect }
            };

        private static RollNode AlwaysApply(List<EffectInvocation> effects, string narrative = "")
            => new RollNode { successEffects = effects, successNarrative = narrative };

        private static RollNode Save(
            SaveSpec spec, int dc,
            List<EffectInvocation> onFail,
            List<EffectInvocation> onPass = null,
            string successNarrative = "",
            string failureNarrative = "",
            RollNode failChain = null)
        {
            spec.dc = dc;
            return new RollNode
            {
                rollSpec = spec,
                failureEffects = onFail ?? new List<EffectInvocation>(),
                successEffects = onPass ?? new List<EffectInvocation>(),
                successNarrative = successNarrative,
                failureNarrative = failureNarrative,
                onFailureChain = failChain
            };
        }

        private static RollNode Check(
            SkillCheckSpec spec, int dc,
            List<EffectInvocation> onSuccess,
            List<EffectInvocation> onFail = null,
            string successNarrative = "",
            string failureNarrative = "",
            RollNode successChain = null)
        {
            spec.dc = dc;
            return new RollNode
            {
                rollSpec = spec,
                successEffects = onSuccess ?? new List<EffectInvocation>(),
                failureEffects = onFail ?? new List<EffectInvocation>(),
                successNarrative = successNarrative,
                failureNarrative = failureNarrative,
                onSuccessChain = successChain
            };
        }

        private static CardChoice Choice(string text, RollNode rollNode)
            => new CardChoice { choiceText = text, rollNode = rollNode };

        private static ChoiceCard Make(string name, string narrative, params CardChoice[] choices)
        {
            var card = ScriptableObject.CreateInstance<ChoiceCard>();
            card.name = name;
            card.cardName = name;
            card.narrativeText = narrative;
            card.choices = new List<CardChoice>(choices);
            return card;
        }
        #endregion
    }
}
#endif
