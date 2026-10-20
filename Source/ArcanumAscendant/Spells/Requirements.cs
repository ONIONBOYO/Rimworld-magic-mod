using RimWorld;
using Verse;

namespace ArcanumAscendant
{
    /// <summary>
    /// Who a colony spell may be cast on. Checked when the target is picked (so mana and cooldown are not wasted on a
    /// target the spell can do nothing for) and again for every pawn an effect touches.
    /// </summary>
    public class PawnRequirement
    {
        public bool humanlike;
        public bool animal;
        public bool mechanoid;
        public bool notMechanoid;
        /// <summary>Must be a member of the caster's faction (colonists, colony animals and mechs).</summary>
        public bool ownFaction;
        public bool prisoner;
        /// <summary>Minimum biological age in years. Romance and fertility magic only ever touches adults.</summary>
        public float minAge = -1f;
        public float maxAge = -1f;
        public bool child;
        public bool pregnant;
        public bool notPregnant;
        public bool female;
        public bool hasLovePartner;
        public bool mage;
        public bool notSelf;
        /// <summary>Biotech must be active (genes, babies, mechanitors).</summary>
        public bool biotech;

        public bool Met(Pawn p, Pawn caster, out string reason)
        {
            reason = null;
            if (p == null)
            {
                reason = "AA_Req_NeedPawn".Translate();
                return false;
            }
            if (biotech && !ModsConfig.BiotechActive)
            {
                reason = "AA_Req_Biotech".Translate();
                return false;
            }
            if (notSelf && p == caster)
            {
                reason = "AA_Req_NotSelf".Translate();
                return false;
            }
            if (humanlike && !p.RaceProps.Humanlike)
            {
                reason = "AA_Req_Humanlike".Translate();
                return false;
            }
            if (animal && !p.RaceProps.Animal)
            {
                reason = "AA_Req_Animal".Translate();
                return false;
            }
            if (mechanoid && !p.RaceProps.IsMechanoid)
            {
                reason = "AA_Req_Mech".Translate();
                return false;
            }
            if (notMechanoid && p.RaceProps.IsMechanoid)
            {
                reason = "AA_Req_NotMech".Translate();
                return false;
            }
            if (ownFaction && (caster == null || p.Faction != caster.Faction))
            {
                reason = "AA_Req_OwnFaction".Translate();
                return false;
            }
            if (prisoner && !p.IsPrisonerOfColony)
            {
                reason = "AA_Req_Prisoner".Translate();
                return false;
            }
            float age = p.ageTracker?.AgeBiologicalYearsFloat ?? 0f;
            if (minAge >= 0f && age < minAge)
            {
                reason = "AA_Req_MinAge".Translate(minAge.ToString("F0"));
                return false;
            }
            if (maxAge >= 0f && age > maxAge)
            {
                reason = "AA_Req_MaxAge".Translate(maxAge.ToString("F0"));
                return false;
            }
            if (child && (!p.RaceProps.Humanlike || p.DevelopmentalStage.Adult()))
            {
                reason = "AA_Req_Child".Translate();
                return false;
            }
            if (pregnant && !IsPregnant(p))
            {
                reason = "AA_Req_Pregnant".Translate();
                return false;
            }
            if (notPregnant && IsPregnant(p))
            {
                reason = "AA_Req_NotPregnant".Translate();
                return false;
            }
            if (female && p.gender != Gender.Female)
            {
                reason = "AA_Req_Female".Translate();
                return false;
            }
            if (hasLovePartner && LovePartnerRelationUtility.ExistingLovePartner(p, false) == null)
            {
                reason = "AA_Req_Partner".Translate();
                return false;
            }
            if (mage && !p.IsMage())
            {
                reason = "AA_Req_Mage".Translate();
                return false;
            }
            return true;
        }

        public static bool IsPregnant(Pawn p)
        {
            var set = p?.health?.hediffSet;
            if (set == null) return false;
            foreach (var h in set.hediffs)
                if (h is Hediff_Pregnant || h is Hediff_Labor || h is Hediff_LaborPushing) return true;
            return false;
        }
    }
}
