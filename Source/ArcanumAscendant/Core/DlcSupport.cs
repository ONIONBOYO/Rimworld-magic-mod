using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ArcanumAscendant
{
    /// <summary>
    /// Shortcuts for DLC content. Anything from a DLC is looked up by name at runtime so the mod loads (and the spell does
    /// something sensible) without it.
    /// </summary>
    public static class Dlc
    {
        public static bool Biotech => ModsConfig.BiotechActive;
        public static bool Ideology => ModsConfig.IdeologyActive;
        public static bool Royalty => ModsConfig.RoyaltyActive;
        public static bool Anomaly => ModsConfig.AnomalyActive;

        public static T Def<T>(string defName) where T : Def => defName.NullOrEmpty() ? null : DefDatabase<T>.GetNamedSilentFail(defName);
    }

    /// <summary>
    /// Stat changes for a hediff's stages that name a stat only a DLC defines (Biotech's Fertility...). XML stat lists can't
    /// mention a stat that might not exist, so these are added to the stages at startup when the stat is there.
    /// </summary>
    public class DlcStatModifiers : DefModExtension
    {
        public List<DlcStatEntry> entries = new List<DlcStatEntry>();
    }

    public class DlcStatEntry
    {
        public string stat;
        /// <summary>Stage index, or -1 for every stage.</summary>
        public int stage = -1;
        public bool factor;
        public float value;
    }

    [StaticConstructorOnStartup]
    public static class DlcStatResolver
    {
        static DlcStatResolver()
        {
            int added = 0;
            foreach (var def in DefDatabase<HediffDef>.AllDefsListForReading)
            {
                var ext = def.GetModExtension<DlcStatModifiers>();
                if (ext?.entries == null || def.stages == null) continue;
                foreach (var e in ext.entries)
                {
                    var stat = Dlc.Def<StatDef>(e.stat);
                    if (stat == null) continue;
                    for (int i = 0; i < def.stages.Count; i++)
                    {
                        if (e.stage >= 0 && e.stage != i) continue;
                        var st = def.stages[i];
                        var list = e.factor ? st.statFactors ??= new List<StatModifier>() : st.statOffsets ??= new List<StatModifier>();
                        list.RemoveAll(m => m.stat == stat);
                        list.Add(new StatModifier { stat = stat, value = e.value });
                        added++;
                    }
                }
            }
            if (added > 0 && Prefs.DevMode) Log.Message("[Arcanum Ascendant] Added " + added + " DLC stat modifiers to statuses.");
        }
    }
}
