using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using HarmonyLib;
using RimWorld;
using Verse;

namespace InventoryTools
{
    [HarmonyPatch(typeof(StatWorker), "GetValueUnfinalized")]
    internal static class StatWorker_GetValueUnfinalized_Patch
    {
        private static readonly MethodInfo StatOffsetFromGearMethod = AccessTools.Method(typeof(StatWorker), "StatOffsetFromGear");
        private static readonly MethodInfo PassiveOffsetMethod = AccessTools.Method(typeof(PassiveInventoryToolCache), "GetOffsetForStat");
        private static readonly FieldInfo StatField = AccessTools.Field(typeof(StatWorker), "stat");
        private static readonly FieldInfo PawnStoryField = AccessTools.Field(typeof(Pawn), "story");

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            int lastGearOffsetCall = FindLastCall(codes, StatOffsetFromGearMethod);
            if (lastGearOffsetCall < 0)
            {
                Log.Error("[Inventory Tools] Could not find StatWorker.StatOffsetFromGear call. Passive inventory tool offsets were not patched.");
                return codes;
            }

            int insertIndex = FindPawnStoryLoadAfter(codes, lastGearOffsetCall);
            if (insertIndex < 0)
            {
                Log.Error("[Inventory Tools] Could not find pawn stat offset insertion point. Passive inventory tool offsets were not patched.");
                return codes;
            }

            List<CodeInstruction> injected = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Ldloc_1),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, StatField),
                new CodeInstruction(OpCodes.Call, PassiveOffsetMethod),
                new CodeInstruction(OpCodes.Add),
                new CodeInstruction(OpCodes.Stloc_0)
            };

            injected[0].labels.AddRange(codes[insertIndex].labels);
            codes[insertIndex].labels.Clear();
            codes.InsertRange(insertIndex, injected);
            return codes;
        }

        private static int FindLastCall(List<CodeInstruction> codes, MethodInfo method)
        {
            int result = -1;
            for (int i = 0; i < codes.Count; i++)
            {
                if ((codes[i].opcode == OpCodes.Call || codes[i].opcode == OpCodes.Callvirt) && SameMethod(codes[i].operand as MethodInfo, method))
                {
                    result = i;
                }
            }

            return result;
        }

        private static int FindPawnStoryLoadAfter(List<CodeInstruction> codes, int startIndex)
        {
            for (int i = startIndex + 1; i < codes.Count - 1; i++)
            {
                if (codes[i].opcode == OpCodes.Ldloc_1 && codes[i + 1].opcode == OpCodes.Ldfld && SameField(codes[i + 1].operand as FieldInfo, PawnStoryField))
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool SameMethod(MethodInfo a, MethodInfo b)
        {
            return a != null && b != null && a.MetadataToken == b.MetadataToken && a.Module == b.Module;
        }

        private static bool SameField(FieldInfo a, FieldInfo b)
        {
            return a != null && b != null && a.MetadataToken == b.MetadataToken && a.Module == b.Module;
        }
    }

    [HarmonyPatch(typeof(StatWorker), "GetOffsetsAndFactorsExplanation")]
    internal static class StatWorker_GetOffsetsAndFactorsExplanation_Patch
    {
        public static void Postfix(StatRequest req, StringBuilder sb, string whitespace, StatDef ___stat, StatWorker __instance)
        {
            if (!req.HasThing)
            {
                return;
            }

            Pawn pawn = req.Thing as Pawn;
            if (pawn == null)
            {
                return;
            }

            List<StatOffsetSource> sources = PassiveInventoryToolCache.GetSourcesForStat(pawn, ___stat);
            if (sources == null || sources.Count == 0)
            {
                return;
            }

            for (int i = 0; i < sources.Count; i++)
            {
                StatOffsetSource source = sources[i];
                sb.AppendLine(whitespace + "InventoryTools_StatsReport_PassiveTool".Translate(source.ItemLabel) + ": " + __instance.ValueToString(source.Value, false, ToStringNumberSense.Offset));
            }
        }
    }
}
