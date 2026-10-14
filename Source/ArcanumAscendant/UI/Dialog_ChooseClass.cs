using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ArcanumAscendant
{
    /// <summary>Class selection screen shown when a pawn awakens.</summary>
    public class Dialog_ChooseClass : Window
    {
        private readonly CompArcane comp;
        private ArcaneClassDef selected;
        private Vector2 detailScroll;
        private readonly List<ArcaneClassDef> classes;

        public override Vector2 InitialSize => new Vector2(Mathf.Min(1180f, UI.screenWidth - 40f), Mathf.Min(820f, UI.screenHeight - 40f));

        public Dialog_ChooseClass(CompArcane comp, bool mustChoose = false)
        {
            this.comp = comp;
            classes = ArcaneClassDef.RootsFor(comp.Pawn);
            selected = classes.FirstOrDefault();
            doCloseX = !mustChoose;
            closeOnCancel = !mustChoose;
            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 36f), "AA_ChooseClassTitle".Translate(comp.Pawn.LabelShort));
            Text.Font = GameFont.Small;
            GUI.color = ColoredText.SubtleGrayColor;
            Widgets.Label(new Rect(inRect.x, inRect.y + 34f, inRect.width, 22f), "AA_ChooseClassSub".Translate());
            GUI.color = Color.white;

            var body = new Rect(inRect.x, inRect.y + 62f, inRect.width, inRect.height - 62f);
            var left = new Rect(body.x, body.y, body.width * 0.52f, body.height);
            var right = new Rect(left.xMax + 12f, body.y, body.width - left.width - 12f, body.height);

            int cols = classes.Count > 12 ? 4 : 3;
            int rows = Mathf.CeilToInt(classes.Count / (float)cols);
            float cw = (left.width - (cols - 1) * 8f) / cols;
            float ch = Mathf.Min(150f, (left.height - (rows - 1) * 8f) / rows);
            for (int i = 0; i < classes.Count; i++)
            {
                var cls = classes[i];
                var r = new Rect(left.x + (i % cols) * (cw + 8f), left.y + (i / cols) * (ch + 8f), cw, ch);
                DrawCard(r, cls);
            }

            if (selected != null) DrawDetails(right, selected);
        }

        private void DrawCard(Rect r, ArcaneClassDef cls)
        {
            bool sel = cls == selected;
            Widgets.DrawBoxSolid(r, new Color(0.07f, 0.06f, 0.1f));
            if (sel)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.realtimeSinceStartup * 3f);
                GUI.color = cls.color * new Color(1, 1, 1, 0.25f + 0.2f * pulse);
                GUI.DrawTexture(r.ExpandedBy(6f), ArcaneTex.NodeGlow);
                GUI.color = Color.white;
            }
            Widgets.DrawBoxSolidWithOutline(r, Color.clear, sel ? cls.color : cls.color * new Color(1, 1, 1, 0.35f), sel ? 2 : 1);
            var icon = new Rect(r.center.x - 34f, r.y + 10f, 68f, 68f);
            GUI.color = cls.color * new Color(1, 1, 1, 0.35f);
            GUI.DrawTexture(icon.ExpandedBy(10f), ArcaneTex.NodeGlow);
            GUI.color = Color.white;
            GUI.DrawTexture(icon, cls.Icon);
            Text.Anchor = TextAnchor.UpperCenter;
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(r.x, icon.yMax + 4f, r.width, 24f), cls.LabelCap.Colorize(cls.color));
            Text.Font = GameFont.Tiny;
            GUI.color = ColoredText.SubtleGrayColor;
            Widgets.Label(new Rect(r.x + 4f, icon.yMax + 24f, r.width - 8f, r.height - icon.height - 30f), cls.tagline);
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            if (Mouse.IsOver(r)) Widgets.DrawHighlight(r);
            if (Widgets.ButtonInvisible(r))
            {
                selected = cls;
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
            }
        }

        private void DrawDetails(Rect r, ArcaneClassDef cls)
        {
            Widgets.DrawBoxSolid(r, new Color(0.05f, 0.045f, 0.08f));
            Widgets.DrawBoxSolidWithOutline(r, Color.clear, cls.color * new Color(1, 1, 1, 0.5f));
            var inner = r.ContractedBy(12f);
            var buttonRect = new Rect(inner.x, inner.yMax - 42f, inner.width, 42f);
            var view = new Rect(inner.x, inner.y, inner.width, inner.height - 50f);

            var content = new Rect(0f, 0f, view.width - 16f, 1400f);
            Widgets.BeginScrollView(view, ref detailScroll, content);
            float y = 0f;
            var icon = new Rect(0f, y, 72f, 72f);
            GUI.DrawTexture(icon, cls.Icon);
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(84f, y + 4f, content.width - 84f, 34f), cls.LabelCap.Colorize(cls.color));
            Text.Font = GameFont.Small;
            GUI.color = ColoredText.SubtleGrayColor;
            Widgets.Label(new Rect(84f, y + 38f, content.width - 84f, 40f), cls.tagline);
            GUI.color = Color.white;
            y += 82f;
            y = Para(content.width, y, cls.description);
            if (!cls.playstyle.NullOrEmpty())
            {
                y = Header(content.width, y, "AA_Playstyle".Translate(), cls.color);
                y = Para(content.width, y, cls.playstyle);
            }
            if (cls.primaryAttribute != null)
                y = Para(content.width, y, "AA_KeyAttributes".Translate(cls.primaryAttribute.label, cls.secondaryAttribute?.label ?? "-"));
            y = Header(content.width, y, "AA_StartingSpells".Translate(), cls.color);
            foreach (var s in cls.startingSpells)
            {
                var row = new Rect(0f, y, content.width, 36f);
                GUI.DrawTexture(new Rect(0f, y, 32f, 32f), s.uiIcon);
                Widgets.Label(new Rect(40f, y + 6f, content.width - 40f, 26f), s.LabelCap);
                TooltipHandler.TipRegion(row, () => ArcaneUtility.SpellTooltip(s, comp.Pawn), s.GetHashCode());
                y += 36f;
            }
            y = Header(content.width, y, "AA_Subclasses".Translate(cls.subclassUnlockLevel), cls.color);
            foreach (var sub in cls.subclasses)
            {
                GUI.DrawTexture(new Rect(0f, y, 40f, 40f), sub.Icon);
                Widgets.Label(new Rect(48f, y, content.width - 48f, 22f), sub.LabelCap.Colorize(sub.color));
                GUI.color = ColoredText.SubtleGrayColor;
                Text.Font = GameFont.Tiny;
                float h = Text.CalcHeight(sub.tagline, content.width - 48f);
                Widgets.Label(new Rect(48f, y + 20f, content.width - 48f, h), sub.tagline);
                Text.Font = GameFont.Small;
                GUI.color = Color.white;
                y += Mathf.Max(46f, 24f + h + 4f);
            }
            if (!cls.lore.NullOrEmpty())
            {
                y = Header(content.width, y, "AA_Lore".Translate(), cls.color);
                GUI.color = new Color(0.8f, 0.78f, 0.7f);
                y = Para(content.width, y, cls.lore);
                GUI.color = Color.white;
            }
            Widgets.EndScrollView();

            if (Widgets.ButtonText(buttonRect, "AA_AwakenAs".Translate(cls.label)))
            {
                comp.Awaken(cls);
                Close();
                Find.WindowStack.Add(new Dialog_Grimoire(comp));
            }
        }

        private static float Header(float w, float y, string text, Color c)
        {
            y += 6f;
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(0f, y, w, 24f), text.Colorize(c));
            Widgets.DrawLineHorizontal(0f, y + 23f, w, c * new Color(1, 1, 1, 0.4f));
            return y + 28f;
        }

        private static float Para(float w, float y, string text)
        {
            if (text.NullOrEmpty()) return y;
            float h = Text.CalcHeight(text, w);
            Widgets.Label(new Rect(0f, y, w, h), text);
            return y + h + 6f;
        }
    }
}
