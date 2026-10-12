using RimWorld;
using UnityEngine;
using Verse;

namespace ArcanumAscendant
{
    /// <summary>Mana / ward / XP gizmo. Click it to open the grimoire.</summary>
    [StaticConstructorOnStartup]
    public class Gizmo_ArcaneStatus : Gizmo
    {
        private readonly CompArcane comp;
        private static readonly Texture2D BgTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.06f, 0.05f, 0.1f, 0.92f));

        public Gizmo_ArcaneStatus(CompArcane comp)
        {
            this.comp = comp;
            Order = -120f;
        }

        public override float GetWidth(float maxWidth) => 212f;

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            var rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
            var cls = comp.DisplayClass;
            GUI.DrawTexture(rect, BgTex);
            Widgets.DrawBoxSolidWithOutline(rect, Color.clear, cls.color * new Color(1, 1, 1, 0.6f));
            var inner = rect.ContractedBy(5f);

            var iconRect = new Rect(inner.x, inner.y, 40f, 40f);
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.realtimeSinceStartup * 2.5f);
            bool pending = comp.perkPoints > 0 || comp.attributePoints > 0 || comp.CanChooseSubclass;
            if (pending)
            {
                GUI.color = cls.color * new Color(1, 1, 1, 0.35f + 0.4f * pulse);
                GUI.DrawTexture(iconRect.ExpandedBy(8f), ArcaneTex.NodeGlow);
                GUI.color = Color.white;
            }
            GUI.DrawTexture(iconRect, cls.Icon);

            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperLeft;
            var labelRect = new Rect(iconRect.xMax + 5f, inner.y - 2f, inner.width - 45f, 18f);
            Widgets.Label(labelRect, (cls.LabelCap + " " + "AA_LevelShort".Translate(comp.level)).Colorize(cls.color));
            if (comp.paragon > 0)
            {
                Text.Anchor = TextAnchor.UpperRight;
                Widgets.Label(labelRect, ("✦" + comp.paragon).Colorize(new Color(1f, 0.85f, 0.4f)));
                Text.Anchor = TextAnchor.UpperLeft;
            }

            float barX = iconRect.xMax + 5f;
            float barW = inner.xMax - barX;
            var manaRect = new Rect(barX, inner.y + 16f, barW, 14f);
            Widgets.FillableBar(manaRect, comp.ManaMax > 0 ? comp.mana / comp.ManaMax : 0f, ArcaneTex.ManaFill, ArcaneTex.ManaBg, false);
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(manaRect, comp.mana.ToString("F0") + " / " + comp.ManaMax.ToString("F0"));
            float wardMax = comp.WardMax;
            var wardRect = new Rect(barX, manaRect.yMax + 2f, barW, 8f);
            if (wardMax > 0.5f) Widgets.FillableBar(wardRect, comp.ward / wardMax, ArcaneTex.WardFill, ArcaneTex.BarBg, false);

            var xpRect = new Rect(inner.x, inner.yMax - 18f, inner.width, 5f);
            Widgets.FillableBar(xpRect, Mathf.Clamp01(comp.xp / comp.XpToNextLevel), ArcaneTex.XpFill, ArcaneTex.BarBg, false);
            var bottom = new Rect(inner.x, inner.yMax - 13f, inner.width, 14f);
            Text.Anchor = TextAnchor.MiddleLeft;
            string pts = "";
            if (comp.perkPoints > 0) pts += "AA_PerkPointsShort".Translate(comp.perkPoints) + "  ";
            if (comp.attributePoints > 0) pts += "AA_AttrPointsShort".Translate(comp.attributePoints) + "  ";
            if (comp.CanChooseSubclass) pts += "AA_SubclassReadyShort".Translate();
            Widgets.Label(bottom, pts.NullOrEmpty() ? "AA_OpenGrimoireShort".Translate().Colorize(ColoredText.SubtleGrayColor) : pts.Colorize(new Color(1f, 0.85f, 0.4f)));
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            TooltipHandler.TipRegion(rect, () => "AA_StatusTip".Translate(comp.Pawn.LabelShort, cls.label, comp.level, comp.xp.ToString("F0"), comp.XpToNextLevel.ToString("F0"),
                comp.mana.ToString("F0"), comp.ManaMax.ToString("F0"), comp.ManaRegenPerSecond.ToString("F1"), comp.ward.ToString("F0"), wardMax.ToString("F0"),
                comp.SpellPower.ToStringPercent()), 77123411);
            if (Mouse.IsOver(rect)) Widgets.DrawHighlight(rect);
            if (Widgets.ButtonInvisible(rect))
            {
                Find.WindowStack.Add(new Dialog_Grimoire(comp));
                return new GizmoResult(GizmoState.Interacted);
            }
            return new GizmoResult(GizmoState.Clear);
        }
    }
}
