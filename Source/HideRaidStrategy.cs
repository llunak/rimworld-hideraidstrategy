using HarmonyLib;
using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace HideRaidStrategy
{
    [StaticConstructorOnStartup]
    public class HarmonyPatches
    {
        static HarmonyPatches()
        {
            var harmony = new Harmony("llunak.HideRaidStrategy");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    [HarmonyPatch]
    public static class IncidentWorker_Generic_Patch
    {
        [HarmonyTargetMethods]
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(IncidentWorker_RaidEnemy), "GetLetterText");
            yield return AccessTools.Method(typeof(IncidentWorker_ShamblerAssault), "GetLetterText");
        }

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> GetLetterText(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            bool found = false;
            for( int i = 0; i < codes.Count; ++i )
            {
                // Log.Message("T:" + i + ":" + codes[i].opcode + "::" + (codes[i].operand != null ? codes[i].operand.ToString() : codes[i].operand));
                // The function has code:
                // text += "\n\n";
                // text += parms.raidStrategy.arrivalTextEnemy;
                // Remove it.
                if( codes[ i ].IsLdloc()
                    && i + 9 < codes.Count
                    && codes[ i + 1 ].opcode == OpCodes.Ldstr && codes[ i + 1 ].operand.ToString() == "\n\n"
                    && codes[ i + 2 ].opcode == OpCodes.Call
                    && codes[ i + 2 ].operand.ToString() == "System.String Concat(System.String, System.String)"
                    && codes[ i + 3 ].IsStloc()
                    && codes[ i + 4 ].IsLdloc()
                    && codes[ i + 5 ].IsLdarg()
                    && codes[ i + 6 ].opcode == OpCodes.Ldfld && codes[ i + 6 ].operand.ToString() == "RimWorld.RaidStrategyDef raidStrategy"
                    && codes[ i + 7 ].opcode == OpCodes.Ldfld && codes[ i + 7 ].operand.ToString() == "System.String arrivalTextEnemy"
                    && codes[ i + 8 ].opcode == OpCodes.Call
                    && codes[ i + 8 ].operand.ToString() == "System.String Concat(System.String, System.String)"
                    && codes[ i + 9 ].IsStloc())
                {
                    codes.RemoveRange( i, 10 );
                    found = true;
                    break;
                }
            }
            if( !found )
                Log.Error("HideRaidStrategy: Failed to patch GetLetterText()");
            return codes;
        }
    }

    [HarmonyPatch(typeof(IncidentWorker_PsychicRitualSiege))]
    public static class IncidentWorker_PsychicRitualSiege_Patch
    {
        [HarmonyTranspiler]
        [HarmonyPatch(nameof(GetLetterText))]
        public static IEnumerable<CodeInstruction> GetLetterText(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            bool found = false;
            int blockStart = -1;
            for( int i = 0; i < codes.Count; ++i )
            {
                // Log.Message("T:" + i + ":" + codes[i].opcode + "::" + (codes[i].operand != null ? codes[i].operand.ToString() : codes[i].operand));
                // The function has code:
                // taggedString += "\n\n";
                // if (!parms.psychicRitualDef.letterAIArrivedText.NullOrEmpty())
                //     taggedString += parms.psychicRitualDef.letterAIArrivedText;
                // else
                //     taggedString += parms.raidStrategy.arrivalTextEnemy.Formatted(parms.psychicRitualDef.label.Named("RITUAL"));
                // Remove it.
                if( blockStart == -1
                    && codes[ i ].IsLdloc()
                    && i + 3 < codes.Count
                    && codes[ i + 1 ].opcode == OpCodes.Ldstr && codes[ i + 1 ].operand.ToString() == "\n\n"
                    && codes[ i + 2 ].opcode == OpCodes.Call
                    && codes[ i + 2 ].operand.ToString() == "Verse.TaggedString op_Addition(Verse.TaggedString, System.String)"
                    && codes[ i + 3 ].IsStloc())
                {
                    blockStart = i;
                }
                if( blockStart != -1
                    && codes[ i ].opcode == OpCodes.Ldstr && codes[ i ].operand.ToString() == "RITUAL"
                    && i + 4 < codes.Count
                    && codes[ i + 1 ].opcode == OpCodes.Call
                    && codes[ i + 2 ].opcode == OpCodes.Call
                    && codes[ i + 3 ].opcode == OpCodes.Call
                    && codes[ i + 3 ].operand.ToString() == "Verse.TaggedString op_Addition(Verse.TaggedString, Verse.TaggedString)"
                    && codes[ i + 4 ].IsStloc())
                {
                    codes.RemoveRange( blockStart, i + 4 - blockStart + 1 );
                    found = true;
                    break;
                }
            }
            if( !found )
                Log.Error("HideRaidStrategy: Failed to patch IncidentWorker_PsychicRitualSiege.GetLetterText()");
            return codes;
        }
    }
}
