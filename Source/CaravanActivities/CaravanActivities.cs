using HarmonyLib;
using System;
using RimWorld;
using Verse;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using RimWorld.Planet;
using System.Text;
using Verse.Sound;
using System.Reflection;
using System.Reflection.Emit;

namespace CaravanActivities
{

    [StaticConstructorOnStartup]
    public static class Patches
    {
        public static MethodInfo TryExecuteWorker = AccessTools.Method(typeof(IncidentWorker_RaidEnemy), "TryExecuteWorker", null, null);
        public static FieldInfo pausedSetter = AccessTools.Field(typeof(Caravan_PathFollower), "paused");
        public static FieldInfo cachedMatSetter = AccessTools.Field(typeof(Caravan), "cachedMat");
            



        static Patches()
        {
            Harmony harmony = new Harmony("temmie3754.caravanactivities");
            harmony.Patch(AccessTools.Method(typeof(Caravan), "GetInspectString", null, null), null, new HarmonyMethod(Patches.patchType, "Patch_GetInspectString", null), null, null);
            harmony.Patch(AccessTools.Method(typeof(Caravan), "GetGizmos", null, null), null, new HarmonyMethod(Patches.patchType, "Patch_GetGizmos", null), null, null);
            harmony.Patch(AccessTools.Method(typeof(MapParent), "GetFloatMenuOptions", null, null), null, new HarmonyMethod(Patches.patchType, "Patch_GetFloatMenuOptions", null), null, null);
            harmony.Patch(AccessTools.Method(typeof(WorldSelector), "AutoOrderToTile", null, null), new HarmonyMethod(Patches.patchType, "Patch_AutoOrderToTile", null), null, null);
            harmony.Patch(AccessTools.Method(typeof(Settlement), "GetFloatMenuOptions", null, null), null, new HarmonyMethod(Patches.patchType, "Patch_GetFloatMenuOptionsSettlement", null), null, null);
            harmony.Patch(AccessTools.Method(typeof(WorldObject), "Draw", null, null), new HarmonyMethod(Patches.patchType, "Patch_Draw", null), null, null);

        }
        public static bool Patch_Draw(WorldObject __instance, MaterialPropertyBlock ___propertyBlock)
        {
            ActivityHandlerComp loadingComp = __instance.GetComponent<ActivityHandlerComp>();
            if (loadingComp != null && loadingComp.doingActivity)
            {
                float averageTileSize = Find.WorldGrid.averageTileSize;
                float transitionPct = ExpandableWorldObjectsUtility.TransitionPct;
                if (__instance.def.expandingIcon && transitionPct > 0f)
                {
                    Color color = __instance.Material.color;
                    float num = 1f - transitionPct;
                    ___propertyBlock.SetColor(ShaderPropertyIDs.Color, new Color(color.r, color.g, color.b, color.a * num));
                    UprightDrawQuadTangentialToPlanet(__instance.DrawPos, 0.7f * averageTileSize, 0.015f, __instance.Material, counterClockwise: false, useSkyboxLayer: false, ___propertyBlock);
                }
                else
                {
                    UprightDrawQuadTangentialToPlanet(__instance.DrawPos, 0.7f * averageTileSize, 0.015f, __instance.Material);
                }
                return false;
            }
            return true;
        }
        public static void UprightDrawQuadTangentialToPlanet(Vector3 pos, float size, float altOffset, Material material, bool counterClockwise = false, bool useSkyboxLayer = false, MaterialPropertyBlock propertyBlock = null)
        {
            if (material == null)
            {
                Log.Warning("Tried to draw quad with null material.");
                return;
            }

            Vector3 normalized = pos.normalized;
            Vector3 vector = ((!counterClockwise) ? normalized : (-normalized));
            Quaternion q = Quaternion.LookRotation(Vector3.Cross(vector, Vector3.up), vector) * Quaternion.Euler(0f, -90f, 0f);
            Vector3 s = new Vector3(size, 1f, size);
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(pos + normalized * altOffset, q, s);
            int layer = (useSkyboxLayer ? WorldCameraManager.WorldSkyboxLayer : WorldCameraManager.WorldLayer);
            if (propertyBlock != null)
            {
                Graphics.DrawMesh(MeshPool.plane10, matrix, material, layer, null, 0, propertyBlock);
            }
            else
            {
                Graphics.DrawMesh(MeshPool.plane10, matrix, material, layer);
            }
        }
        public static void Patch_GetInspectString(Caravan __instance, ref string __result)
        {
            ActivityHandlerComp loadingComp = __instance.GetComponent<ActivityHandlerComp>();
            if (loadingComp != null && loadingComp.doingActivity)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine();
                sb.AppendLine("CaravanActivites_ActivityLabel".Translate(loadingComp.caravanActivityDef.label, loadingComp.caravanActivityDef.options[loadingComp.selectedOption].label));
                sb.Append("CaravanActivites_DayProgress".Translate(( 1 - ((loadingComp.nextTick - Find.TickManager.TicksGame) / 60000f) + loadingComp.days).ToString("F1"), loadingComp.caravanActivityDef.options[loadingComp.selectedOption].requiredTimeInDays));
                __result += sb.ToString();
            }
        }

        public static bool Patch_AutoOrderToTile(Caravan c, int tile)
        {
            ActivityHandlerComp loadingComp = c.GetComponent<ActivityHandlerComp>();
            if (loadingComp == null || !loadingComp.doingActivity) return true;
            return false;
        }
        public static IEnumerable<FloatMenuOption> Patch_GetFloatMenuOptions(IEnumerable<FloatMenuOption> values, Caravan caravan, MapParent __instance)
        {
            ActivityHandlerComp loadingComp = caravan.GetComponent<ActivityHandlerComp>();

            foreach (FloatMenuOption option in values)
            {

                if (loadingComp == null || !loadingComp.doingActivity) yield return option;
            }
        }
        public static IEnumerable<FloatMenuOption> Patch_GetFloatMenuOptionsSettlement(IEnumerable<FloatMenuOption> values, Caravan caravan)
        {
            ActivityHandlerComp loadingComp = caravan.GetComponent<ActivityHandlerComp>();
            foreach (FloatMenuOption option in values)
            {
                if (loadingComp == null || !loadingComp.doingActivity) yield return option;
            }
        }
        public static IEnumerable<Gizmo> Patch_GetGizmos(IEnumerable<Gizmo> values, Caravan __instance)
        {
            ActivityHandlerComp activityHandler = __instance.GetComponent<ActivityHandlerComp>();
            if (activityHandler != null && activityHandler.doingActivity)
            {
                yield return new Command_Action
                {
                    icon = TexCommand.ClearPrioritizedWork,
                    defaultLabel = "Cancel activity",
                    defaultDesc = "Cancels currently ongoing activity",
                    action = delegate { activityHandler.doingActivity = false; }
                };
                if (DebugSettings.godMode)
                {
                    yield return new Command_Action
                    {
                        icon = TexUI.ArrowTexRight,
                        defaultLabel = "Skip acitivity day",
                        defaultDesc = "Makes the activity perform its daily check and update accordingly",
                        action = delegate { activityHandler.nextTick = Find.TickManager.TicksGame + 10; }
                    };
                    int result;
                    int weightsum;
                    yield return new Command_Action
                    {
                        icon = TexUI.ArrowTexRight,
                        defaultLabel = "Finish (good)",
                        defaultDesc = "End and perform one of the good outcomes for the activity",
                        action = delegate {
                            activityHandler.doingActivity = false;
                            List<ActivityOutcomeChanceClass> goodOutcomes = activityHandler.caravanActivityDef.options[activityHandler.selectedOption].goodOutcomes;
                            weightsum = goodOutcomes.Sum(o => o.chance);
                            result = Rand.RangeInclusive(0, weightsum);
                            weightsum = 0;
                            foreach (ActivityOutcomeChanceClass outcome in goodOutcomes)
                            {
                                weightsum += outcome.chance;
                                if (weightsum >= result)
                                {
                                    CaravanActivityUtility.performOutcome(activityHandler.caravanActivityDef, __instance, activityHandler.selectedOption, outcome.outcome, true);
                                    return;
                                }
                            }
                        }
                    };
                    yield return new Command_Action
                    {
                        icon = TexUI.ArrowTexRight,
                        defaultLabel = "Finish (bad)",
                        defaultDesc = "End and perform one of the bad outcomes for the activity",
                        action = delegate { activityHandler.doingActivity = false;
                            List<ActivityOutcomeChanceClass> badOutcomes = activityHandler.caravanActivityDef.options[activityHandler.selectedOption].badOutcomes;
                            weightsum = badOutcomes.Sum(o => o.chance);
                            result = Rand.RangeInclusive(0, weightsum);
                            weightsum = 0;
                            foreach (ActivityOutcomeChanceClass outcome in badOutcomes)
                            {
                                weightsum += outcome.chance;
                                if (weightsum >= result)
                                {
                                    CaravanActivityUtility.performOutcome(activityHandler.caravanActivityDef, __instance, activityHandler.selectedOption, outcome.outcome, false);
                                    return;
                                }
                            }
                        }
                    };
                }
            }
            else
            {
                foreach (Gizmo gizmo in values)
                {
                    yield return gizmo;
                }
                if (activityHandler == null) { yield break; }
                foreach (CaravanActivityDef activityDef in CaravanActivityUtility.GetCaravanActivities(Find.World, __instance))
                {
                    yield return CaravanActivityUtility.GetGizmoFromActivity(activityDef, __instance);
                }
            }
            
            
        }

        private static readonly Type patchType = typeof(Patches);
    }

    public static class CaravanActivityUtility
    {
        public static List<CaravanActivityDef> AllActivities = DefDatabase<CaravanActivityDef>.AllDefsListForReading;
        public static List<CaravanActivityDef> GetCaravanActivities(World world, Caravan caravan)
        {
            List<CaravanActivityDef> caravanActivitiesList = new List<CaravanActivityDef>();
            if (Find.WorldObjects.Caravans.Any(c => c != caravan && c.Tile == caravan.Tile && c.GetComponent<ActivityHandlerComp>() != null && c.GetComponent<ActivityHandlerComp>().doingActivity == true)) return caravanActivitiesList;
            foreach (CaravanActivityDef activity in AllActivities)
            {
                if (activity.road && Find.WorldGrid.tiles[caravan.Tile].Roads.NullOrEmpty()) continue;
                if (activity.river && Find.WorldGrid.tiles[caravan.Tile].Rivers.NullOrEmpty()) continue;
                if (!activity.biomes.NullOrEmpty() && !activity.biomes.Contains(caravan.Biome)) continue;
                if (!activity.features.NullOrEmpty() && !activity.features.Contains(Find.WorldGrid.tiles[caravan.Tile].feature.def)) continue;
                if (activity.nearFactionBase)
                {
                    List<int> neighbourtiles = new List<int>();
                    world.grid.GetTileNeighbors(caravan.Tile, neighbourtiles);
                    neighbourtiles.Add(caravan.Tile);
                    if (!neighbourtiles.Where(tile => world.worldObjects.AnySettlementAt(tile))
                        .Any(tile => Find.WorldObjects.SettlementAt(tile).Faction != null && !Find.WorldObjects.SettlementAt(tile).Faction.IsPlayer && ( activity.allowedFactionTechLevel.NullOrEmpty() || activity.allowedFactionTechLevel.Contains(Find.WorldObjects.SettlementAt(tile).Faction.def.techLevel) ))) continue;
                }
                if (activity.minPawnCount > caravan.pawns.Count) continue;
                caravanActivitiesList.Add(activity);
            }
            return caravanActivitiesList;
        }

        public static Gizmo GetGizmoFromActivity(CaravanActivityDef activity, Caravan caravan)
        {
            return new Command_Action
            {
                icon = activity.gizmoTexture,
                defaultLabel = activity.label,
                defaultDesc = activity.description,
                action = delegate { opengui(activity, caravan); }
            };
        }

        public static void opengui(CaravanActivityDef activity, Caravan caravan)
        {
            Find.WindowStack.Add(new Dialog_CaravanActivity(activity, caravan));
        }
        public static void doactivity(CaravanActivityDef activity, Caravan caravan, int selectedOption)
        {
            Patches.pausedSetter.SetValue(caravan.pather, true);
            caravan.Notify_DestinationOrPauseStatusChanged();
            ActivityHandlerComp activityHandler = caravan.GetComponent<ActivityHandlerComp>();
            activityHandler.caravanActivityDef = activity;
            activityHandler.doingActivity = true;

            float riskMultiplier = 1;
            if (!activity.options[selectedOption].boostItems.NullOrEmpty())
            {
                List<Pawn> pawns = caravan.PawnsListForReading.Where(p =>
                {
                    bool skillchecker = true;
                    if (p.GetType() != typeof(Pawn)) return false;
                    foreach (ActivitySkillLevelClass skill in activity.options[selectedOption].minSkillLevels)
                    {
                        if (p.skills.GetSkill(skill.skill).levelInt < skill.level) skillchecker = false;
                    }
                    return skillchecker;
                }).ToList();
               
                foreach (BoostItemDef boost in activity.options[selectedOption].boostItems)
                {
                    if (boost.riskMultiplier != 1)
                    {
                        if (CaravanInventoryUtility.AllInventoryItems(caravan).Any(t => t.def == boost.thing && t.stackCount >= boost.count * (boost.pawnScale ? pawns.Count : 1))) riskMultiplier *= boost.riskMultiplier;
                    }
                }
            }
            activityHandler.badChancePerDay = (1 - Mathf.Pow( 1 - (activity.options[selectedOption].negativeOutcomeChance * riskMultiplier)  / 100f, 1 / activity.options[selectedOption].requiredTimeInDays));
            activityHandler.nextTick = Find.TickManager.TicksGame + 60000;
            activityHandler.selectedOption = selectedOption;
        }

        public static void performOutcome(CaravanActivityDef activity, Caravan caravan, int selectedOption, OutcomeDef outcome, bool positiveOutcome)
        {
            List<Pawn> pawns = caravan.PawnsListForReading.Where( p => {
                bool skillchecker = true;
                if (p.GetType() != typeof(Pawn)) return false;
                foreach (ActivitySkillLevelClass skill in activity.options[selectedOption].minSkillLevels)
                {
                    if (p.skills.GetSkill(skill.skill).levelInt < skill.level) skillchecker = false;
                }
                return skillchecker;
            }
            ).ToList();
            bool activityend = false;
            StringBuilder sb = new StringBuilder();
            if (outcome.colonistsInjured != null)
            {
                int injureXColonists = Rand.RangeInclusive(outcome.colonistsInjured.min, outcome.colonistsInjured.max);

                if (injureXColonists > 0)
                {
                    if (injureXColonists > caravan.PawnsListForReading.Count) injureXColonists = caravan.PawnsListForReading.Count;
                    List<Pawn> ColonistsToKill = caravan.PawnsListForReading.OrderBy(_ => Rand.Int).Take(injureXColonists).ToList();
                    foreach (Pawn pawn in ColonistsToKill)
                    {
                        pawn.TakeDamage(new DamageInfo(DamageDefOf.Crush, 15f));
                    }
                    sb.AppendLine("CaravanActivites_InjuredPawns".Translate(injureXColonists));
                }
            }
            if (outcome.colonistsKilled != null ) 
            {
                int killXColonists = Rand.RangeInclusive(outcome.colonistsKilled.min, outcome.colonistsKilled.max);
                
                if (killXColonists > 0)
                {
                    if (killXColonists > caravan.PawnsListForReading.Count) killXColonists = caravan.PawnsListForReading.Count;
                    List<Pawn> ColonistsToKill = caravan.PawnsListForReading.OrderBy(_ => Rand.Int).Take(killXColonists).ToList();
                    foreach (Pawn pawn in ColonistsToKill)
                    {
                        pawn.Kill(null);
                    }
                    sb.AppendLine("CaravanActivites_KilledPawns".Translate(killXColonists));
                    if (caravan.pawns.Count - killXColonists < activity.minPawnCount) activityend = true;
                }
            }
            if (outcome.items != null)
            {
                for (int i = 0; i < outcome.items.Count; i++)
                {
                    ThingDef stuff;
                    Thing thing;
                    ThingCatDefCountRangeClass caravanActivityLoot = outcome.items[i];
                    int count = caravanActivityLoot.countRange.RandomInRange;
                    if (outcome.skillScale)
                    {
                        float multiplier;
                        List<float> multiplierlist = new List<float>();
                        if (!activity.options[selectedOption].recSkillLevels.NullOrEmpty())
                        {
                            foreach (ActivitySkillLevelClass skill in activity.options[selectedOption].recSkillLevels)
                            {
                                float averageSkill = (float)pawns.Average(pawn => pawn.skills.GetSkill(skill.skill).levelInt);
                                Mathf.Clamp(averageSkill, 0, 20);
                                if (averageSkill <= skill.level)
                                {
                                    multiplierlist.Add(0.5f + 0.5f * averageSkill / skill.level);
                                }
                                else
                                {
                                    multiplierlist.Add(2 * (averageSkill - skill.level) / (20 - skill.level));
                                }
                            }
                            multiplier = multiplierlist.Average();
                            Log.Message(multiplier.ToString());

                            count = Mathf.RoundToInt(count * multiplier);
                        } 
                    }

                    float itemMultiplier = 1;
                    if (!activity.options[selectedOption].boostItems.NullOrEmpty())
                    {
                        foreach (BoostItemDef boost in activity.options[selectedOption].boostItems)
                        {
                            if (boost.itemMultiplier != 1)
                            {
                                if (CaravanInventoryUtility.AllInventoryItems(caravan).Any(t => t.def == boost.thing && t.stackCount >= boost.count * (boost.pawnScale ? pawns.Count : 1 ))) itemMultiplier *= boost.itemMultiplier;
                            }
                        }
                    }

                    if (activity.options[selectedOption].itemMultiplier != 1) count = Mathf.RoundToInt(count * activity.options[selectedOption].itemMultiplier);
                    count = Mathf.RoundToInt(count * itemMultiplier);
                    int stacks = count / caravanActivityLoot.thingDef.stackLimit + 1;
                    while (stacks > 1)
                    {
                        stuff = GenStuff.RandomStuffFor(caravanActivityLoot.thingDef);
                        thing = ThingMaker.MakeThing(caravanActivityLoot.thingDef, stuff);
                        thing.stackCount = thing.def.stackLimit;
                        CaravanInventoryUtility.FindPawnToMoveInventoryTo(thing, caravan.PawnsListForReading, null).inventory.TryAddAndUnforbid(thing);
                        stacks -= 1;
                    }
                    stuff = GenStuff.RandomStuffFor(caravanActivityLoot.thingDef);
                    thing = ThingMaker.MakeThing(caravanActivityLoot.thingDef, stuff);
                    thing.stackCount = count % thing.def.stackLimit;
                    CaravanInventoryUtility.FindPawnToMoveInventoryTo(thing, caravan.PawnsListForReading, null).inventory.TryAddAndUnforbid(thing);
                    sb.AppendLine("CaravanActivites_ObtainItem".Translate(thing.def.label.CapitalizeFirst(), count));
                }
            }
            int pollutionChange = outcome.tilePollutionChange.RandomInRange;
            if (pollutionChange != 0)
            {
                if (pollutionChange > 0)
                {
                    WorldPollutionUtility.PolluteWorldAtTile(caravan.Tile, pollutionChange / 100f);
                }

                else
                {

                    WorldPollutionUtility.PolluteWorldAtTile(caravan.Tile, pollutionChange / 100f);
                    /*float newPol = Mathf.Clamp01( Find.WorldGrid.tiles[caravan.Tile].pollution + pollutionChange);
                    Find.WorldGrid.tiles[caravan.Tile].pollution = newPol;
                    Find.World.renderer.Notify_TilePollutionChanged(caravan.Tile)*/;
                }
                //Find.WorldGrid.tiles[caravan.Tile].pollution = Mathf.Clamp(Find.WorldGrid.tiles[caravan.Tile].pollution + pollutionChange / 100f, 0f, 1f);
                sb.AppendLine("CaravanActivites_PollutionChange".Translate(pollutionChange));
            }
            int relationChange = outcome.nearestFactionRelationshipChange.RandomInRange;
            if (relationChange != 0)
            {
                List<Settlement> settlements = Find.WorldObjects.SettlementBases;
                settlements.SortBy(x => Find.WorldGrid.ApproxDistanceInTiles(caravan.Tile, x.Tile));
                if (settlements.Count > 0)
                {
                    Faction.OfPlayer.TryAffectGoodwillWith(settlements.First().Faction, relationChange);
                    sb.AppendLine("CaravanActivites_GoodwillChange".Translate(settlements.First().Faction.Name, relationChange));
                }
            }

            if (outcome.raid != null)
            {
                Map map;
                if (outcome.raid.attacksCurrentLocation)  map = GetOrGenerateMapUtility.GetOrGenerateMap(caravan.Tile, WorldObjectDefOf.Ambush);
                else map = Find.RandomPlayerHomeMap;
                if (outcome.raid.mechs)
                {
                    MechClusterSketch mechCluster = MechClusterGenerator.GenerateClusterSketch(outcome.raid.points, map);
                    MechClusterUtility.SpawnCluster(MechClusterUtility.FindClusterPosition(map, mechCluster), map, mechCluster);
                    sb.AppendLine("CaravanActivites_MechRaid".Translate());
                }
                else
                {
                    IncidentParms incidentParms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, map);
                    incidentParms.points = outcome.raid.points;
                    if (outcome.raid.faction != null) incidentParms.faction = Find.FactionManager.FirstFactionOfDef(outcome.raid.faction);
                    if (outcome.raid.nearestFaction == true)
                    {
                        List<Settlement> settlements = Find.WorldObjects.SettlementBases;
                        settlements.SortBy(x => Find.WorldGrid.ApproxDistanceInTiles(caravan.Tile, x.Tile));
                        if (settlements.Count > 0)
                        {
                            incidentParms.faction = settlements.First().Faction;
                        }
                    }
                    Find.Storyteller.incidentQueue.Add(
                        IncidentDefOf.RaidEnemy,
                        Find.TickManager.TicksGame + 1,
                        incidentParms
                        );
                    sb.AppendLine("CaravanActivites_OutcomeRaid".Translate());
                }



            }

            if (outcome.changeIdeology)
            {
                List<Settlement> settlements = Find.WorldObjects.SettlementBases;
                settlements.SortBy(x => Find.WorldGrid.ApproxDistanceInTiles(caravan.Tile, x.Tile));
                if (settlements.Count > 0)
                {
                    settlements.First().Faction.ideos.SetPrimary(Faction.OfPlayer.ideos.PrimaryIdeo);
                    sb.AppendLine("CaravanActivites_IdeologyChange".Translate(settlements.First().Faction.Name, Faction.OfPlayer.ideos.PrimaryIdeo.name));
                }
            }

            if (outcome.endsActivity || activityend)
            {
                ActivityHandlerComp loadingComp = caravan.GetComponent<ActivityHandlerComp>();
                loadingComp.doingActivity = false;
                caravan.pather.Paused = false;
                sb.AppendLine();
                sb.AppendLine("CaravanActivites_ActivityEnd".Translate());
                Find.World.GetComponent<TileActivityTracker>().tileCooldowns.Add(caravan.Tile, activity.cooldown * 60000 + Find.TickManager.TicksGame);
            }
            if (positiveOutcome)
            {
                PositiveActivityOutcome letter = (PositiveActivityOutcome)LetterMaker.MakeLetter(DefDatabase<LetterDef>.GetNamed("PositiveActivityOutcome"));
                letter.text = sb.ToString();
                letter.openletter = false;
                letter.title = "CaravanActivites_ActivitySuccess".Translate();
                letter.Label = "CaravanActivites_ActivityOutcome".Translate();
                Find.LetterStack.ReceiveLetter(letter);
            }
            else
            {
                NegativeActivityOutcome letter = (NegativeActivityOutcome)LetterMaker.MakeLetter(DefDatabase<LetterDef>.GetNamed("NegativeActivityOutcome"));
                letter.text = sb.ToString();
                letter.openletter = false;
                letter.title = "CaravanActivites_ActivityNotSuccess".Translate();
                letter.Label = "CaravanActivites_ActivityOutcome".Translate();
                Find.LetterStack.ReceiveLetter(letter);
            }
        }
    }

    public class Dialog_CaravanActivity : Window
    {
        public CaravanActivityDef CaravanActivity;
        public int selectedOption = 0;
        public Caravan caravan;

        public Dialog_CaravanActivity(CaravanActivityDef activity, Caravan caravan)
        {
            this.CaravanActivity = activity;
            this.caravan = caravan;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            listingStandard.Label(CaravanActivity.label);
            listingStandard.Gap();
            listingStandard.Label(CaravanActivity.description);
            listingStandard.Gap();
            
            for (int i = 0; i < CaravanActivity.options.Count; i++)
            {
                StringBuilder optiontooltip = new StringBuilder();
                if (!CaravanActivity.options[i].description.NullOrEmpty())
                {
                    optiontooltip.AppendLine(CaravanActivity.options[i].description);
                }
                optiontooltip.AppendLine("CaravanActivites_ActivityRisk".Translate(CaravanActivity.options[i].negativeOutcomeChance));
                optiontooltip.Append("CaravanActivites_ActivityLength".Translate(CaravanActivity.options[i].requiredTimeInDays));
                if (!CaravanActivity.options[i].minSkillLevels.NullOrEmpty())
                {
                    optiontooltip.AppendLine();
                    optiontooltip.Append("CaravanActivites_MinimumSkills".Translate());
                    foreach (ActivitySkillLevelClass skill in CaravanActivity.options[i].minSkillLevels)
                    {
                        optiontooltip.AppendLine();
                        optiontooltip.Append("{0}: {1}".Formatted(skill.skill.label.CapitalizeFirst(), skill.level));
                    }
                }
                if (!CaravanActivity.options[i].requiredItems.NullOrEmpty())
                {
                    optiontooltip.AppendLine();
                    optiontooltip.Append("CaravanActivites_RequiredItems".Translate());
                    foreach (ThingDefCountClass thing in CaravanActivity.options[selectedOption].requiredItems)
                    {
                        optiontooltip.AppendLine();
                        optiontooltip.Append("{0}: {1}".Formatted(thing.thingDef.label.CapitalizeFirst(), thing.count));
                    }
                }
                
                Rect rectradio = listingStandard.GetRect(Text.CalcHeight(CaravanActivity.options[i].label, listingStandard.ColumnWidth - 12f));
                                

                if (!optiontooltip.ToString().NullOrEmpty())
                {
                    if (Mouse.IsOver(rectradio))
                    {
                        Widgets.DrawHighlight(rectradio);
                    }

                    TipSignal tip =  new TipSignal(optiontooltip.ToString());
                    TooltipHandler.TipRegion(rectradio, tip);
                }

                if (Widgets.RadioButtonLabeled(rectradio, CaravanActivity.options[i].label, selectedOption == i)) selectedOption = i;
                listingStandard.Gap();
                listingStandard.Gap();
            }
            listingStandard.End();
            Rect rect = new Rect(inRect.x, inRect.y + inRect.height - 80, inRect.width, 80);
            listingStandard.Begin(rect);
            if (listingStandard.ButtonText("LetterJoinOfferAccept".Translate()))
            {
                if (!CaravanActivity.options[selectedOption].minSkillLevels.NullOrEmpty())
                {
                    bool skillcheck = false;
                    foreach (ActivitySkillLevelClass skill in CaravanActivity.options[selectedOption].minSkillLevels)
                    {
                        if (caravan.PawnsListForReading.Where(p => p.GetType() == typeof(Pawn) && p.skills.GetSkill(skill.skill).levelInt >= skill.level).Count() < CaravanActivity.minPawnCount) skillcheck = true;
                    }

                    if (skillcheck)
                    {
                        Messages.Message("CaravanActivites_InsufficientSkill".Translate(), MessageTypeDefOf.RejectInput, historical: false);
                        SoundDefOf.ClickReject.PlayOneShotOnCamera();
                        return;
                    }
                    skillcheck = false;
                    foreach (ThingDefCountClass thing in CaravanActivity.options[selectedOption].requiredItems)
                    {
                        if (!CaravanInventoryUtility.AllInventoryItems(caravan).Any(t => t.def == thing.thingDef && t.stackCount >= thing.count)) skillcheck = true;
                    }

                    if (skillcheck)
                    {
                        Messages.Message("CaravanActivites_InsufficientItems".Translate(), MessageTypeDefOf.RejectInput, historical: false);
                        SoundDefOf.ClickReject.PlayOneShotOnCamera();
                        return;
                    }
                }
                if (Find.World.GetComponent<TileActivityTracker>().tileCooldowns.ContainsKey(caravan.Tile))
                {
                    if (Find.TickManager.TicksGame < Find.World.GetComponent<TileActivityTracker>().tileCooldowns[caravan.Tile])
                    {
                        Messages.Message("CaravanActivites_TileCooldown".Translate(((Find.World.GetComponent<TileActivityTracker>().tileCooldowns[caravan.Tile] - Find.TickManager.TicksGame) / 60000f).ToString("F1")), MessageTypeDefOf.RejectInput, historical: false);
                        SoundDefOf.ClickReject.PlayOneShotOnCamera();
                        return;
                    }
                }
                Close();
                CaravanActivityUtility.doactivity(CaravanActivity, caravan, selectedOption);

            }
            if (listingStandard.ButtonText("RejectLetter".Translate())) Close();
            listingStandard.End();
        }
    }
    public class CaravanActivityDef : Def
    {
        public List<BiomeDef> biomes = new List<BiomeDef>();
        public List<OptionDef> options = new List<OptionDef>();
        public List<FeatureDef> features = new List<FeatureDef>();
        public int minPawnCount = 1;
        public string icon;
        public string mapIcon = "World/WorldObjects/Expanding/Sites/Outpost";
        public bool nearFactionBase;
        public int cooldown = 10;
        public bool road = false;
        public bool river = false;
        public List<TechLevel> allowedFactionTechLevel = new List<TechLevel>();
        public Texture gizmoTexture;
        public Material mapMaterial;

        public CaravanActivityDef()
        {
            LongEventHandler.ExecuteWhenFinished(delegate
            {
                Texture2D tex = ContentFinder<Texture2D>.Get(icon, false);
                if (tex == null)
                {
                    tex = DefDatabase<ThingDef>.GetNamed(icon).uiIcon;
                }
                gizmoTexture = tex;
                tex = ContentFinder<Texture2D>.Get(mapIcon, true);
                /*Texture2D texture = new Texture2D(tex.width, tex.height, tex.format, true);
                Graphics.CopyTexture(tex, texture);
                Color[] colors = texture.GetPixels();
                Color[] newColors = new Color[colors.Length];
                for (int j = 0; j < texture.height; ++j)
                {
                    for (int i = 0; i < texture.width; ++i)
                    {
                        newColors[(i + 1) * texture.height - j - 1] = colors[j * texture.width + i];
                    }
                }
                texture.SetPixels(newColors); */
                mapMaterial = MaterialPool.MatFrom(srcTex: tex, color: new Color(1f, 0.863f, 0.33f), shader: ShaderDatabase.WorldOverlayTransparentLit, renderQueue: WorldMaterials.DynamicObjectRenderQueue);
            });
        }

    }
    public class OptionDef : Def
    {   
        public List<ActivitySkillLevelClass> minSkillLevels = new List<ActivitySkillLevelClass>();
        public List<ActivitySkillLevelClass> recSkillLevels = new List<ActivitySkillLevelClass>();
        public List<ThingDefCountClass> requiredItems = new List<ThingDefCountClass>();
        public List<BoostItemDef> boostItems = new List<BoostItemDef>();
        public int negativeOutcomeChance; // overall chance, behind the scenes will deconstruct this to a per day chance so it can fail before the required time
        // every day will roll the die to see if a negative outcome occurs, defined in the possibleOutcomes, negative outcome will occur and if the outcome ends the activity or results in the caravan being
        // unable to finish, the activity will stop.
        public int requiredTimeInDays;
        public float itemMultiplier = 1;
        public List<ActivityOutcomeChanceClass> goodOutcomes = new List<ActivityOutcomeChanceClass>();
        public List<ActivityOutcomeChanceClass> badOutcomes = new List<ActivityOutcomeChanceClass>();

    }
    
    public class BoostItemDef : Def
    {
        public ThingDef thing;
        public int count = 1;
        public float riskMultiplier = 1;
        public float itemMultiplier = 1;
        public bool pawnScale = true;
    }

    public class OutcomeDef : Def
    {
        public bool skillScale = false; // Enables reward based outcomes to scale with skill
        public List<ThingCatDefCountRangeClass> items = new List<ThingCatDefCountRangeClass>();
        public RaidDef raid = null;
        public IntRange nearestFactionRelationshipChange = new IntRange(0, 0);
        public IntRange tilePollutionChange = new IntRange(0, 0);
        public bool endsActivity = true;
        public IntRange colonistsInjured = new IntRange(0, 0);
        public IntRange colonistsKilled = new IntRange(0, 0);
        public bool changeIdeology = false;
    }

    public class RaidDef : Def
    {
        public bool attacksCurrentLocation = false; // if true raid will be at caravan, otherwise at home base
        public int points;
        public bool mechs = false;
        public bool nearestFaction = false;
        public FactionDef faction;
    }
   

    public class ActivityHandlerComp : WorldObjectComp
    {
        public int selectedOption;
        public CaravanActivityDef caravanActivityDef;
        public bool _doingActivity = false;
        public bool doingActivity { 
            get {
                return _doingActivity;
            }
            set
            {
                _doingActivity = value;
                if (_doingActivity)
                {
                    Patches.cachedMatSetter.SetValue(parent, caravanActivityDef.mapMaterial);
                }
                else
                {
                    Patches.cachedMatSetter.SetValue(parent, null);
                }
            }
        
        }
        public int nextTick = 0;
        public int days = 0;
        public float badChancePerDay = 0;
        public override void CompTick()
        {
            if (!doingActivity) return;
            if (Find.TickManager.TicksGame < nextTick) return;
            nextTick += 60000;
            days += 1;
            int weightsum;
            int result;
            if (Rand.Chance(badChancePerDay))
            {
                List<ActivityOutcomeChanceClass> badOutcomes = caravanActivityDef.options[selectedOption].badOutcomes;
                weightsum = badOutcomes.Sum(o => o.chance);
                result = Rand.RangeInclusive(0, weightsum);
                weightsum = 0;
                foreach (ActivityOutcomeChanceClass outcome in badOutcomes)
                {
                    weightsum += outcome.chance;
                    if (weightsum >= result)
                    {
                        CaravanActivityUtility.performOutcome(caravanActivityDef, (Caravan)parent, selectedOption, outcome.outcome, false);
                        return;
                    }
                }
            }
            if (days >= caravanActivityDef.options[selectedOption].requiredTimeInDays)
            {
                List<ActivityOutcomeChanceClass> goodOutcomes = caravanActivityDef.options[selectedOption].goodOutcomes;
                weightsum = goodOutcomes.Sum(o => o.chance);
                result = Rand.RangeInclusive(0, weightsum);
                weightsum = 0;
                foreach (ActivityOutcomeChanceClass outcome in goodOutcomes)
                {
                    weightsum += outcome.chance;
                    if (weightsum >= result)
                    {
                        CaravanActivityUtility.performOutcome(caravanActivityDef, (Caravan)parent, selectedOption, outcome.outcome, true);
                        doingActivity = false;
                        return;
                    }
                }
            }
        }


        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref selectedOption, "selectedOption", 0);
            Scribe_Defs.Look(ref caravanActivityDef, "caravanActivityDef");
            Scribe_Values.Look(ref _doingActivity, "_doingActivity", false);
            Scribe_Values.Look(ref nextTick, "nextTick", 0);
            Scribe_Values.Look(ref days, "days", 0);
            Scribe_Values.Look(ref badChancePerDay, "badChancePerDay", 0);
        }
        public CompProperties_ActivityHandler Props => (CompProperties_ActivityHandler)this.props;

    }

    public class CompProperties_ActivityHandler : WorldObjectCompProperties
    {
        public CompProperties_ActivityHandler() 
        { 
            this.compClass = typeof(ActivityHandlerComp);

        }

    }

    public class PositiveActivityOutcome : Letter
    {
        public string text;
        public string title;
        public bool openletter = true;
        public override bool ShouldAutomaticallyOpenLetter => openletter;
        public override void OpenLetter()
        {
            Dialog_ActivityOutcome window = new Dialog_ActivityOutcome(text, title);
            window.letter = this;
            Find.WindowStack.Add(window);
        }

        protected override string GetMouseoverText()
        {
            return title;
        }
    }
    
    public class NegativeActivityOutcome : Letter
    {
        public string text;
        public string title;
        public bool openletter = true;
        public override bool ShouldAutomaticallyOpenLetter => openletter;
        public override void OpenLetter()
        {            
            Dialog_ActivityOutcome window = new Dialog_ActivityOutcome(text, title);
            window.letter = this;
            Find.WindowStack.Add(window);
        }

        protected override string GetMouseoverText()
        {
            return title;
        }
    }

    public class Dialog_ActivityOutcome : Window
    {
        public string text;
        public string title;
        public Letter letter;
        public Dialog_ActivityOutcome(string text, string title) : base()
        {
            this.text = text;
            this.title = title;
            doCloseButton = true;
        }
        public override void PostClose()
        {
            base.PostClose();
            Find.LetterStack.RemoveLetter(letter);
        }
        public override void DoWindowContents(Rect inRect)
        {
            Listing_Standard listing_Standard = new Listing_Standard();
            listing_Standard.Begin(inRect);
            listing_Standard.Label(title);
            listing_Standard.Gap();
            listing_Standard.Label(text);
            
            listing_Standard.End();
        }
    }

    public class TileActivityTracker : WorldComponent
    {
        public Dictionary<int, int> tileCooldowns = new Dictionary<int, int>();
        public TileActivityTracker(World world) : base(world)
        {
        }

    }
}

