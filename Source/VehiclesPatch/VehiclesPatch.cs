using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using Verse;
using HarmonyLib;
using Vehicles;
using CaravanActivities;
using UnityEngine;


namespace VehiclesPatch
{
    [StaticConstructorOnStartup]
    public static class Patches
    {
        static Patches()
        {
            Harmony harmony = new Harmony("temmie3754.caravanactivities");
            harmony.Patch(AccessTools.Method(typeof(VehicleCaravan), "GetInspectString", null, null), null, new HarmonyMethod(Patches.patchType, "Patch_GetInspectString", null), null, null);
            harmony.Patch(AccessTools.Method(typeof(VehicleCaravan), "Draw", null, null), new HarmonyMethod(Patches.patchType, "Patch_Draw", null), null, null);
            harmony.Patch(AccessTools.Method(typeof(VehicleCaravan), "Tick", null, null), null, new HarmonyMethod(Patches.patchType, "Patch_Tick", null), null, null);


        }
        public static void Patch_Tick(VehicleCaravan __instance)
        {
            ActivityHandlerComp loadingComp = __instance.GetComponent<ActivityHandlerComp>();
            if (loadingComp != null)
            {
                if (loadingComp.doingActivity)
                {
                    if (__instance.vPather.Moving) __instance.vPather.Paused = true;

                }
            }
        }

        public static bool Patch_Draw(VehicleCaravan __instance, MaterialPropertyBlock ___propertyBlock)
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
                    UprightDrawQuadTangentialToPlanet(__instance.DrawPos, 0.7f * averageTileSize, 0.015f, loadingComp.caravanActivityDef.mapMaterial, counterClockwise: false, useSkyboxLayer: false, ___propertyBlock);
                }
                else
                {
                    UprightDrawQuadTangentialToPlanet(__instance.DrawPos, 0.7f * averageTileSize, 0.015f, loadingComp.caravanActivityDef.mapMaterial);
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
        public static void Patch_GetInspectString(VehicleCaravan  __instance, ref string __result)
        {
            ActivityHandlerComp loadingComp = __instance.GetComponent<ActivityHandlerComp>();
            if (loadingComp != null && loadingComp.doingActivity)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine();
                sb.AppendLine("Activity: {0} ({1})".Formatted(loadingComp.caravanActivityDef.label, loadingComp.caravanActivityDef.options[loadingComp.selectedOption].label));
                sb.Append("{0}/{1} days".Formatted((1 - ((loadingComp.nextTick - Find.TickManager.TicksGame) / 60000f) + loadingComp.days).ToString("F1"), loadingComp.caravanActivityDef.options[loadingComp.selectedOption].requiredTimeInDays));
                __result += sb.ToString();
            }
        }

        public static bool Patch_AutoOrderToTile(Caravan c, int tile)
        {
            ActivityHandlerComp loadingComp = c.GetComponent<ActivityHandlerComp>();
            if (!loadingComp.doingActivity) return true;
            return false;
        }
        public static IEnumerable<FloatMenuOption> Patch_GetFloatMenuOptions(IEnumerable<FloatMenuOption> values, Caravan caravan, MapParent __instance)
        {
            ActivityHandlerComp loadingComp = caravan.GetComponent<ActivityHandlerComp>();

            foreach (FloatMenuOption option in values)
            {

                if (!loadingComp.doingActivity) yield return option;
            }
        }
        public static IEnumerable<FloatMenuOption> Patch_GetFloatMenuOptionsSettlement(IEnumerable<FloatMenuOption> values, Caravan caravan)
        {
            ActivityHandlerComp loadingComp = caravan.GetComponent<ActivityHandlerComp>();

            foreach (FloatMenuOption option in values)
            {
                if (!loadingComp.doingActivity) yield return option;
            }
        }

        private static readonly Type patchType = typeof(Patches);
    }
}
