using HarmonyLib;
using UnityEngine;
using System;
using System.Reflection;
using System.Collections.Generic;
using RimWorld;
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
        private static HashSet<Type> raidLikeEvents = new HashSet<Type>();

        public static Settings settings { get { return _settings; }}
        public static bool IsRaidLikeEvent( Type type )
        {
            return raidLikeEvents.Contains( type );
        }

        public HideRaidStrategyMod( ModContentPack content )
            : base( content )
        {
            _settings = GetSettings< Settings >();
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

        // Support for other mods here.

        // If a mod adds a new raid type that uses IncidentWorker_RaidEnemy, then this mod should usually
        // work automatically. The other mod should not override functions like GetLetterText() or GetLetterDef(),
        // if it does, it should call IsMaskRaidLikeEventsEnabled() and return base implementation if true.

        // If a mod adds a raid-like event that should be masked as a raid if configured so, then it can
        // use IsMaskRaidLikeEventsEnabled() to detect such a case and act accordingly. SendLetter() can
        // be used in cases when the event should act like a raid.

        // If a mod's raid type uses IncidentWorker_RaidEnemy but should act as a raid-like, i.e. follow the setting,
        // then in addition to what is above the mod should also call RegisterRaidLikeEvent() to prevent this mod
        // from possibly overriding its letter.

        // This function should be called from other mods to get the setting.
        // If a mod implements another raid type that should be always enabled if this mod is active,
        // the mere presence of this function can be used to detect that.
        public static bool IsMaskRaidLikeEventsEnabled()
        {
            return settings.maskRaidLikeEvents;
        }

        // A helper to get a generic letter text, if needed.
        // This is mostly a copy&paste of IncidentWorker_RaidEnemy code, to hide the raid specifics,
        // and also part of IncidentWorker_Raid.TryExecuteWorker().
        public static void SendLetter(IncidentWorker worker, IncidentParms parms, List<Pawn> pawns)
        {
            TaggedString letterLabel = parms.faction.Name;
            PawnsArrivalModeDef raidArrivalMode = parms.raidArrivalMode ?? PawnsArrivalModeDefOf.EdgeWalkIn;
            TaggedString text = string.Format(raidArrivalMode.textEnemy, parms.faction.def.pawnsPlural, parms.faction.Name.ApplyTag(parms.faction)).CapitalizeFirst();
            // Arrival text removed here.
            Pawn pawn = pawns.Find((Pawn x) => x.Faction.leader == x);
            if (pawn != null)
            {
                text += "\n\n";
                text += "EnemyRaidLeaderPresent".Translate(pawn.Faction.def.pawnsPlural, pawn.LabelShort, pawn.Named("LEADER")).Resolve();
            }
            if (parms.raidAgeRestriction != null && !parms.raidAgeRestriction.arrivalTextExtra.NullOrEmpty())
            {
                text += "\n\n";
                text += parms.raidAgeRestriction.arrivalTextExtra.Formatted(parms.faction.def.pawnsPlural.Named("PAWNSPLURAL")).Resolve();
            }
            TaggedString relatedText = "LetterRelatedPawnsRaidEnemy".Translate(Faction.OfPlayer.def.pawnsPlural, parms.faction.def.pawnsPlural);
            PawnRelationUtility.Notify_PawnsSeenByPlayer_Letter(pawns, ref letterLabel, ref text, relatedText, informEvenIfSeenBefore: true);
            worker.SendStandardLetter(letterLabel, text, LetterDefOf.ThreatBig, parms, pawns);
        }

        // Call with the type of the IncidentWorker_RaidEnemy derived class if the event is supposed to be masked
        // as a raid per the configurable option.
        public static void RegisterRaidLikeEvent(Type eventType)
        {
            raidLikeEvents.Add( eventType );
        }
    }
}
