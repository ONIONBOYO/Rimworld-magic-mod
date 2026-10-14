using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ArcanumAscendant
{
    public class CompProperties_ArcaneDeflector : CompProperties
    {
        /// <summary>Radius of the dome. Grows a little with the caster's power.</summary>
        public float radius = 3.5f;
        /// <summary>Fraction of a stopped shot's damage hurled back at whoever fired it.</summary>
        public float reflectFraction;
        public DamageDef reflectDamageDef;
        public Color color = new Color(0.6f, 1f, 0.9f);
        public bool drawDome = true;

        public CompProperties_ArcaneDeflector()
        {
            compClass = typeof(CompArcaneDeflector);
        }
    }

    /// <summary>
    /// A dome of hard wind around a construct. Hostile projectiles fired from outside are torn apart as they cross it
    /// (mortar shells included), and stronger domes throw part of the force back at the shooter.
    /// </summary>
    public class CompArcaneDeflector : ThingComp
    {
        private static readonly List<Projectile> buffer = new List<Projectile>();
        private int lastDeflectTick = -9999;
        private int deflected;

        public CompProperties_ArcaneDeflector Props => (CompProperties_ArcaneDeflector)props;

        private CompArcaneConstruct Construct => parent.TryGetComp<CompArcaneConstruct>();

        public float Radius => Props.radius * Mathf.Clamp(Mathf.Sqrt(Mathf.Max(0.1f, Construct?.power ?? 1f)), 0.85f, 1.6f);

        private Faction Owner => parent.Faction ?? Construct?.caster?.Faction;

        private bool Hostile(Thing launcher)
        {
            var owner = Owner;
            if (launcher == null) return owner != null;
            if (owner == null) return false;
            if (launcher.Faction != null) return launcher.Faction.HostileTo(owner);
            return launcher is Pawn p && p.HostileTo(owner);
        }

        public override void CompTick()
        {
            base.CompTick();
            if (!parent.Spawned) return;
            var map = parent.Map;
            if (Props.drawDome && map == Find.CurrentMap && parent.IsHashIntervalTick(9))
            {
                Vector3 edge = parent.TrueCenter() + VRand.Dir(VRand.Range(0f, 360f)) * Radius;
                Fx.Burst(map, edge, Props.color * new Color(1f, 1f, 1f, 0.7f), 1f, PMat.Gust, 1.2f, 0.4f, 0.45f, PFlags.Stretch | PFlags.AlignVel, 0f, 0f, 0.1f, 0.1f);
            }
            var list = map.listerThings.ThingsInGroup(ThingRequestGroup.Projectile);
            if (list.Count == 0) return;
            Vector3 c = parent.TrueCenter();
            float r = Radius;
            float r2 = r * r;
            buffer.Clear();
            foreach (var t in list)
            {
                if (!(t is Projectile proj) || proj.Destroyed || !proj.Spawned) continue;
                Vector3 pos = proj.ExactPosition;
                if ((pos - c).MagnitudeHorizontalSquared() > r2) continue;
                var launcher = proj.Launcher;
                if (!Hostile(launcher)) continue;
                // shots fired from inside the dome get through: it only stops what comes from outside
                if (launcher != null && launcher.Spawned && launcher.Map == map && (launcher.TrueCenter() - c).MagnitudeHorizontalSquared() <= r2) continue;
                buffer.Add(proj);
            }
            foreach (var proj in buffer) Deflect(proj, c);
            buffer.Clear();
        }

        private void Deflect(Projectile proj, Vector3 center)
        {
            var map = parent.Map;
            Vector3 pos = proj.ExactPosition;
            var launcher = proj.Launcher;
            float dmg = 0f;
            try
            {
                dmg = proj.DamageAmount;
            }
            catch
            {
                dmg = 10f;
            }
            proj.Destroy(DestroyMode.Vanish);
            deflected++;
            lastDeflectTick = Find.TickManager.TicksGame;
            if (map == Find.CurrentMap)
            {
                Vector3 outward = (pos - center).Yto0();
                float ang = outward.sqrMagnitude > 0.001f ? outward.AngleFlat() : VRand.Range(0f, 360f);
                Fx.Sparks(map, pos, Props.color, 6f, 4f, 0.1f, 0.3f, ang, 70f, 0f);
                Fx.Burst(map, pos, Props.color, 4f, PMat.Gust, 3f, 0.35f, 0.45f, PFlags.Stretch | PFlags.AlignVel, 0f, 0f, 0.1f, 0.1f);
                ArcaneVfxManager.For(map).Add(new VfxSprite { mat = ArcaneMats.Ring, pos = pos, scaleStart = 0.3f, scaleEnd = 1.4f, duration = 10, color = Props.color });
                ArcaneLightFlash.Spawn(map, pos, Props.color, 2.5f, 6);
            }
            if (Props.reflectFraction > 0f && launcher is Pawn shooter && !shooter.Dead && shooter.Spawned && shooter.Map == map)
            {
                float amount = Mathf.Max(1f, dmg * Props.reflectFraction * Mathf.Max(0.5f, Construct?.power ?? 1f));
                var caster = Construct?.caster;
                CompArcane.applyingSpellDamage = true;
                try
                {
                    shooter.TakeDamage(new DamageInfo(Props.reflectDamageDef ?? ArcaneDefOf.AA_Force, amount, 0.5f, -1f, caster ?? (Thing)parent));
                }
                finally
                {
                    CompArcane.applyingSpellDamage = false;
                }
                if (map == Find.CurrentMap)
                    ArcaneVfxManager.For(map).Add(new VfxProjectile(pos, shooter.DrawPos, 8, ArcaneMats.Get("ArcanumAscendant/Vfx/Gust"), Props.color, Color.white, 0.6f, 0f, false, false)
                        { style = VfxStyle.Wind });
            }
        }

        public override void PostDraw()
        {
            base.PostDraw();
            if (!Props.drawDome) return;
            float now = Find.TickManager.TicksGame;
            float hit = Mathf.Clamp01(1f - (now - lastDeflectTick) / 20f);
            Vector3 c = parent.TrueCenter();
            c.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            float r = Radius;
            float breathe = 1f + 0.02f * Mathf.Sin(now * 0.05f + parent.thingIDNumber);
            Color col = Props.color;
            ArcaneMats.DrawQuad(ArcaneMats.Ring, c, r * 2.05f * breathe, 0f, col * new Color(1f, 1f, 1f, 0.35f + 0.5f * hit));
            ArcaneMats.DrawQuad(ArcaneMats.Get("ArcanumAscendant/Vfx/Vortex"), c + new Vector3(0f, 0.001f, 0f), r * 2f, now * 2.5f, col * new Color(1f, 1f, 1f, 0.12f + 0.2f * hit));
            ArcaneMats.DrawQuad(ArcaneMats.Glow, c + new Vector3(0f, 0.002f, 0f), r * 1.6f, 0f, col * new Color(1f, 1f, 1f, 0.06f + 0.18f * hit));
        }

        public override string CompInspectStringExtra()
        {
            return "AA_DeflectorInspect".Translate(Radius.ToString("F1"), deflected).ToString();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref deflected, "deflected");
        }
    }
}
