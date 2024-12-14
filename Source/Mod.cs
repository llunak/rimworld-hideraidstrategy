using HarmonyLib;
using UnityEngine;
using System.Reflection;
using Verse;

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

    public class Settings : ModSettings
    {
        public bool maskRaidLikeEvents = true;

        public override void ExposeData()
        {
            Scribe_Values.Look( ref maskRaidLikeEvents, "HideRaidStrategy.MaskRaidLikeEvents", true );
        }
    }

    public class HideRaidStrategyMod : Mod
    {
        private static Settings _settings;
        public static Settings settings { get { return _settings; }}

        public HideRaidStrategyMod( ModContentPack content )
            : base( content )
        {
            _settings = GetSettings< Settings >();
        }

        // This function should be called from other mods to get the setting.
        // If a mod implements another raid type that should be always enabled if this mod is active,
        // the mere presence of this function can be used to detect that.
        public static bool IsMaskRaidLikeEventsEnabled()
        {
            return settings.maskRaidLikeEvents;
        }

        public override string SettingsCategory()
        {
            return "Hide Raid Strategy";
        }

        public override void DoSettingsWindowContents(Rect rect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin( rect );
            listing.CheckboxLabeled( "HideRaidStrategy.MaskRaidLikeEvents.Label".Translate(),
                ref settings.maskRaidLikeEvents, "HideRaidStrategy.MaskRaidLikeEvents.Tooltip".Translate());
            listing.End();
            base.DoSettingsWindowContents(rect);
        }
    }
}
