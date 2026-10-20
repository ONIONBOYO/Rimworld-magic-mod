using RimWorld;
using Verse;

namespace ArcanumAscendant
{
    /// <summary>Only shows arcane stats on awakened mages.</summary>
    public class StatWorker_Arcane : StatWorker
    {
        public override bool ShouldShowFor(StatRequest req)
        {
            // Decide on the pawn first. The base check doesn't know the arcana stat category and logs "Unhandled case" for any
            // non-pawn thing (items, buildings, turrets) whose info card is opened, so it must never see those.
            if (!(req.Thing is Pawn p) || !p.IsMage()) return false;
            return base.ShouldShowFor(req);
        }
    }
}
