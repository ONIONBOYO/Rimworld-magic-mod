using RimWorld;
using Verse;

namespace ArcanumAscendant
{
    /// <summary>Only shows arcane stats on awakened mages.</summary>
    public class StatWorker_Arcane : StatWorker
    {
        public override bool ShouldShowFor(StatRequest req)
        {
            // Check for a mage before asking the base worker: for items and buildings it doesn't know the arcana category and logs
            // "Unhandled case" for every arcane stat whenever an info card opens.
            if (!(req.Thing is Pawn p) || !p.IsMage()) return false;
            return base.ShouldShowFor(req);
        }
    }
}
