#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Assets.Scripts.Events.EventCard.EventCardTypes;
using Assets.Scripts.Combat.Rolls.RollSpecs;
using Assets.Scripts.Combat.Rolls.RollSpecs.SpecTypes;
using Assets.Scripts.Combat.Rolls.Targeting;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Characters;
using Assets.Scripts.Combat.Damage;
using Assets.Scripts.Combat.Restoration;
using Assets.Scripts.Combat.Damage.FormulaProviders.SpecificProviders;
using Assets.Scripts.Conditions;
using StatusEffectTemplate = Assets.Scripts.Conditions.EntityConditions.EntityCondition;
using Assets.Scripts.Effects;
using Assets.Scripts.Effects.EffectTypes;
using Assets.Scripts.Effects.EffectTypes.EntityEffects;
using Assets.Scripts.Effects.Invocations;
using Assets.Scripts.Effects.Targeting;
using Assets.Scripts.Effects.Targeting.VehicleTarget;
using Assets.Scripts.Effects.Targeting.EntityTarget;
using Assets.Scripts.Entities.Vehicles.VehicleComponents;

namespace Assets.Scripts.Events.EventCard
{
    /// <summary>
    /// Convenience editor class to create new EventCard assets with preset configurations based on type.
    /// </summary>
    public static class EventCardCreator
    {

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
            RegenerateCard(DefineFlamingDebris());
            RegenerateCard(DefineTarPit());
            RegenerateCard(DefineRestorationStation());

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
                        FX(Dmg(2, 6, 0, DamageType.Bludgeoning, OnSelf)),
                        successNarrative: "You weave through the debris unscathed.",
                        failureNarrative: "Rocks slam into your vehicle.")));

        // Pattern: no roll — unconditional effect applied regardless of choice.
        private static ChoiceCard DefineOilSlick()
            => Make("Oil Slick",
                "The road ahead is slicked with oil.",
                Choice("Power through",
                    AlwaysApply(
                        FX(Dmg(1, 4, 0, DamageType.Physical, OnSelf)),
                        "Your tyres screech as you slide through the slick.")));

        // Pattern: 2-choice — character check gates a bonus; safe fallback costs nothing.
        private static ChoiceCard DefineHiddenShortcut()
            => Make("Hidden Shortcut",
                "Your navigator spots what might be a shortcut through the ruins.",
                Choice("Scout the shortcut",
                    Check(SkillCheckSpec.ForCharacter(CharacterSkill.Perception, ComponentType.Sensors), 13,
                        FX(Energy(3, OnSelf)),
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
                        FX(Dmg(1, 6, 0, DamageType.Physical, OnSelf)),
                        successNarrative: "The crew braces tight — minimal damage.",
                        failureNarrative: "The storm batters the hull.")),
                Choice("Full throttle",
                    AlwaysApply(
                        FX(Dmg(1, 4, 0, DamageType.Physical, OnSelf)),
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
                            FX(Dmg(2, 4, 0, DamageType.Force, OnSelf)),
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
                        successChain: AlwaysApply(FX(Energy(4, OnSelf)), "The salvage yields useful components."))),
                Choice("Leave it",
                    AlwaysApply(FX(), "You keep your eyes on the race.")));

        // Pattern: hazard with combined consequence — fail the save, take fire damage and gain Burning DoT.
        private static ChoiceCard DefineFlamingDebris()
            => Make("Flaming Debris",
                "Burning wreckage scatters across the track!",
                Choice("Power through",
                    Save(SaveSpec.ForVehicle(VehicleCheckAttribute.Stability), 14,
                        FX(Dmg(1, 6, 0, DamageType.Fire, OnSelf),
                           Status(LoadEntityCondition("Burning"), OnSelf)),
                        successNarrative: "You weave through the flames untouched.",
                        failureNarrative: "Your hull catches fire!")));

        // Pattern: unconditional debuff — no roll, Slowed applied to the vehicle.
        private static ChoiceCard DefineTarPit()
            => Make("Tar Pit",
                "The track ahead is thick with adhesive tar. There's no way around it.",
                Choice("Plough through",
                    AlwaysApply(
                        FX(Status(LoadEntityCondition("Slowed"), OnSelf)),
                        "The tar clings to your wheels — you're slowed for the next stretch.")));

        // Pattern: positive event — no roll, apply Regenerating HoT to the vehicle.
        private static ChoiceCard DefineRestorationStation()
            => Make("Restoration Station",
                "A race-sponsor's repair drone swoops alongside your vehicle.",
                Choice("Accept the assist",
                    AlwaysApply(
                        FX(Status(LoadEntityCondition("Regenerating"), OnSelf)),
                        "The drone patches your damage. You'll be back up to speed shortly.")),
                Choice("Wave it off",
                    AlwaysApply(FX(), "You decline the offer and press on.")));

        // Part 3 (ApplyCharacterConditionEffect pending): character condition applied from an event card.
        // private static ChoiceCard DefineInspirationalSign()
        //     => Make("Inspirational Sign",
        //         "A rallying banner hangs across the track — your crew takes heart.",
        //         Choice("Cheer the crew",
        //             AlwaysApply(
        //                 FX(CharacterStatus(LoadCharacterCondition("Inspired"))),
        //                 "The crew feels emboldened.")));

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

        private static StatusEffectTemplate LoadEntityCondition(string name)
            => AssetDatabase.LoadAssetAtPath<StatusEffectTemplate>($"{ConditionCreator.StatusEffectsFolder}/{name}.asset");

        // Part 3 (ApplyCharacterConditionEffect pending): uncomment when CharacterCondition application is implemented.
        // private static CharacterCondition LoadCharacterCondition(string name)
        //     => AssetDatabase.LoadAssetAtPath<CharacterCondition>($"{ConditionCreator.ConditionsFolder}/{name}.asset");

        // ==================== BUILDER METHODS ====================

        private static IVehicleEffectResolver OnSelf => new SourceVehicleResolver();

        private static List<IEffectInvocation> FX(params IEffectInvocation[] effects)
            => new(effects);

        private static IEffectInvocation Dmg(
            int dice, int dieSize, int bonus = 0,
            DamageType type = DamageType.Physical,
            IVehicleEffectResolver target = null)
            => new VehicleEffectInvocation
            {
                targetResolver = target ?? OnSelf,
                effect = new DamageEffect
                {
                    formulaProvider = new StaticFormulaProvider
                    {
                        formula = new DamageFormula { baseDice = dice, dieSize = dieSize, bonus = bonus, damageType = type }
                    }
                }
            };

        private static IEffectInvocation Energy(int amount, IVehicleEffectResolver target = null)
            => new VehicleEffectInvocation
            {
                targetResolver = target ?? OnSelf,
                effect = new ResourceRestorationEffect { formula = new RestorationFormula { resourceType = ResourceType.Energy, isDrain = false, bonus = amount } }
            };

        private static IEffectInvocation Status(StatusEffectTemplate effect, IVehicleEffectResolver target = null)
            => new VehicleEffectInvocation
            {
                targetResolver = target ?? OnSelf,
                effect = new ApplyEntityConditionEffect { condition = effect }
            };

        private static RollNode AlwaysApply(List<IEffectInvocation> effects, string narrative = "")
            => new()
            { targetResolver = new CurrentTargetResolver(), successEffects = effects, successNarrative = narrative };

        private static RollNode Save(
            SaveSpec spec, int dc,
            List<IEffectInvocation> onFail,
            List<IEffectInvocation> onPass = null,
            string successNarrative = "",
            string failureNarrative = "",
            RollNode failChain = null)
        {
            spec.dc = dc;
            return new RollNode
            {
                targetResolver = new CurrentTargetResolver(),
                rollSpec = spec,
                failureEffects = onFail ?? new List<IEffectInvocation>(),
                successEffects = onPass ?? new List<IEffectInvocation>(),
                successNarrative = successNarrative,
                failureNarrative = failureNarrative,
                onFailureChain = failChain
            };
        }

        private static RollNode Check(
            SkillCheckSpec spec, int dc,
            List<IEffectInvocation> onSuccess,
            List<IEffectInvocation> onFail = null,
            string successNarrative = "",
            string failureNarrative = "",
            RollNode successChain = null)
        {
            spec.dc = dc;
            return new RollNode
            {
                targetResolver = new CurrentTargetResolver(),
                rollSpec = spec,
                successEffects = onSuccess ?? new List<IEffectInvocation>(),
                failureEffects = onFail ?? new List<IEffectInvocation>(),
                successNarrative = successNarrative,
                failureNarrative = failureNarrative,
                onSuccessChain = successChain
            };
        }

        private static CardChoice Choice(string text, RollNode rollNode)
            => new()
            { choiceText = text, rollNode = rollNode };

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
