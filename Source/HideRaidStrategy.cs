using HarmonyLib;
using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace HideRaidStrategy
{
    [HarmonyPatch(typeof(IncidentWorker_RaidEnemy))]
    public static class IncidentWorker_RaidEnemy_Patch
    {
        [HarmonyTranspiler]
        [HarmonyPatch(nameof(GetLetterText))]
        public static IEnumerable<CodeInstruction> GetLetterText(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var codes = new List<CodeInstruction>(instructions);
            bool found = false;
            for( int i = 0; i < codes.Count; ++i )
            {
                // Log.Message("T:" + i + ":" + codes[i].opcode + "::" + (codes[i].operand != null ? codes[i].operand.ToString() : codes[i].operand));
                // The function has code:
                // text += "\n\n";
                // text += parms.raidStrategy.arrivalTextEnemy;
                // Place all of it inside:
                // if(GetLetterText_Hook(this))
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
                    Label label = generator.DefineLabel();
                    codes[ i + 10 ].labels.Add( label );
                    codes.Insert( i, new CodeInstruction( OpCodes.Ldarg_0 )); // load 'this'
                    codes.Insert( i + 1, new CodeInstruction( OpCodes.Call,
                        typeof( IncidentWorker_RaidEnemy_Patch ).GetMethod( nameof( GetLetterText_Hook ))));
                    codes.Insert( i + 2, new CodeInstruction( OpCodes.Brfalse, label ));
                    found = true;
                    break;
                }
            }
            if( !found )
                Log.Error("HideRaidStrategy: Failed to patch IncidentWorker_RaidEnemy.GetLetterText()");
            return codes;
        }

        public static bool GetLetterText_Hook(IncidentWorker_RaidEnemy worker)
        {
            return !HideRaidStrategyMod.settings.maskRaidLikeEvents && HideRaidStrategyMod.IsRaidLikeEvent(worker.GetType());
        }

        // Force the letter text to be just the faction name, so that there's no detail like 'Siege'.
        [HarmonyPostfix]
        [HarmonyPatch(nameof(GetLetterLabel))]
        [HarmonyPriority(Priority.VeryLow)]
        public static string GetLetterLabel(string result, IncidentWorker_RaidEnemy __instance, IncidentParms parms)
        {
            if( !HideRaidStrategyMod.settings.maskRaidLikeEvents && HideRaidStrategyMod.IsRaidLikeEvent(__instance.GetType()))
                return result;
            return parms.faction.Name;
        }
    }
}
