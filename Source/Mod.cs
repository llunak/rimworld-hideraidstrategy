using HarmonyLib;
using Verse;
using System.Reflection;

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
}
