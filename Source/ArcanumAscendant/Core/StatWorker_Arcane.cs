using RimWorld;
using Verse;

namespace ArcanumAscendant
{
    /// <summary>Only shows arcane stats on awakened mages.</summary>
    public class StatWorker_Arcane : StatWorker
    {
        public override bool ShouldShowFor(StatRequest req)
        {
            if (!base.ShouldShowFor(req)) return false;
            return req.Thing is Pawn p && p.IsMage();
        }
    }
}
