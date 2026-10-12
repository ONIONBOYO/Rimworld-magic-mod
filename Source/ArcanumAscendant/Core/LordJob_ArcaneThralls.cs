using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace ArcanumAscendant
{
    /// <summary>Summoned, raised and dominated pawns escort and defend the mage who bound them.</summary>
    public class LordJob_ArcaneThralls : LordJob
    {
        public Pawn master;
        public IntVec3 fallback;

        public LordJob_ArcaneThralls() { }

        public LordJob_ArcaneThralls(Pawn master)
        {
            this.master = master;
            fallback = master?.Position ?? IntVec3.Invalid;
        }

        public override bool AddFleeToil => false;
        public override bool CanBlockHostileVisitors => false;

        public override StateGraph CreateGraph()
        {
            var graph = new StateGraph();
            graph.AddToil(new LordToil_ArcaneEscort());
            return graph;
        }

        public override AcceptanceReport AllowsDrafting(Pawn pawn) => false;

        public override void ExposeData()
        {
            Scribe_References.Look(ref master, "master");
            Scribe_Values.Look(ref fallback, "fallback");
        }
    }

    public class LordToil_ArcaneEscort : LordToil
    {
        private int lastUpdate = -9999;

        public override bool AllowSatisfyLongNeeds => false;

        public override void UpdateAllDuties()
        {
            var job = lord.LordJob as LordJob_ArcaneThralls;
            var master = job?.master;
            bool masterOk = master != null && master.Spawned && !master.Dead && master.Map == lord.Map;
            if (masterOk) job.fallback = master.Position;
            foreach (var p in lord.ownedPawns)
            {
                if (p.mindState == null) continue;
                if (masterOk) p.mindState.duty = new PawnDuty(DutyDefOf.Escort, master, 7f);
                else
                {
                    var spot = job != null && job.fallback.IsValid && job.fallback.InBounds(lord.Map) ? job.fallback : p.Position;
                    p.mindState.duty = new PawnDuty(DutyDefOf.Defend, spot, 14f);
                }
            }
        }

        public override void LordToilTick()
        {
            base.LordToilTick();
            int now = Find.TickManager.TicksGame;
            if (now - lastUpdate >= 300)
            {
                lastUpdate = now;
                UpdateAllDuties();
            }
        }
    }
}
