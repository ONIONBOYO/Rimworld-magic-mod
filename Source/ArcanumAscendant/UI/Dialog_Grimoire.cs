using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ArcanumAscendant
{
    /// <summary>The main progression window: attributes, class tree, subclass trees and spellbook.</summary>
    public class Dialog_Grimoire : Window
    {
        private enum Tab { ClassTree, SubclassTree, Spellbook, Codex }

        private readonly CompArcane comp;
        private Tab tab = Tab.ClassTree;
        private ArcaneClassDef previewSubclass;
        private Vector2 treeScroll;
        private Vector2 bookScroll;
        private Vector2 codexScroll;
        private Vector2 sideScroll;
        private float zoom = 1f;
        private const float CellW = 118f;
        private const float CellH = 104f;
        private const float TreeMargin = 70f;
        private static readonly Color PanelBg = new Color(0.045f, 0.04f, 0.075f);
        private static readonly Color Gold = new Color(1f, 0.84f, 0.4f);
        private static readonly Dictionary<Color, Texture2D> barTexCache = new Dictionary<Color, Texture2D>();

        private static Texture2D BarTex(Color c)
        {
            if (!barTexCache.TryGetValue(c, out var t))
            {
                t = SolidColorMaterials.NewSolidColorTexture(c);
                barTexCache[c] = t;
            }
            return t;
        }

        public override Vector2 InitialSize => new Vector2(Mathf.Min(1420f, UI.screenWidth - 30f), Mathf.Min(900f, UI.screenHeight - 30f));
        protected override float Margin => 10f;

        public Dialog_Grimoire(CompArcane comp)
        {
            this.comp = comp;
            doCloseX = true;
            doCloseButton = false;
            forcePause = false;
            absorbInputAroundWindow = false;
            closeOnClickedOutside = false;
            draggable = true;
            resizeable = true;
            preventCameraMotion = false;
            if (comp.CanChooseSubclass) tab = Tab.SubclassTree;
        }

        public override void DoWindowContents(Rect inRect)
        {
            if (comp?.Pawn == null || !comp.IsMage || comp.Pawn.Destroyed)
            {
                Close();
                return;
            }
            var cls = comp.DisplayClass;
            Widgets.DrawBoxSolid(inRect, new Color(0.03f, 0.025f, 0.05f));
            var header = new Rect(inRect.x, inRect.y, inRect.width - 30f, 74f);
            DrawHeader(header, cls);
            var tabsRect = new Rect(inRect.x + 260f, header.yMax + 6f, inRect.width - 260f, 32f);
            DrawTabs(tabsRect);
            var side = new Rect(inRect.x, header.yMax + 6f, 250f, inRect.height - header.height - 6f);
            DrawSidebar(side);
            var main = new Rect(side.xMax + 10f, tabsRect.yMax + 4f, inRect.width - side.width - 10f, inRect.height - header.height - 6f - 36f);
            switch (tab)
            {
                case Tab.ClassTree:
                    DrawTree(main, comp.classDef, true);
                    break;
                case Tab.SubclassTree:
                    if (comp.subclassDef != null) DrawTree(main, comp.subclassDef, true);
                    else if (previewSubclass != null) DrawTreePreview(main, previewSubclass);
                    else DrawSubclassChoice(main);
                    break;
                case Tab.Spellbook:
                    DrawSpellbook(main);
                    break;
                case Tab.Codex:
                    DrawCodex(main);
                    break;
            }
        }

        #region Header & tabs

        private void DrawHeader(Rect r, ArcaneClassDef cls)
        {
            Widgets.DrawBoxSolid(r, PanelBg);
            Widgets.DrawBoxSolidWithOutline(r, Color.clear, cls.color * new Color(1, 1, 1, 0.45f));
            var icon = new Rect(r.x + 8f, r.y + 5f, 64f, 64f);
            GUI.color = cls.color * new Color(1, 1, 1, 0.4f);
            GUI.DrawTexture(icon.ExpandedBy(10f), ArcaneTex.NodeGlow);
            GUI.color = Color.white;
            GUI.DrawTexture(icon, cls.Icon);
            Text.Font = GameFont.Medium;
            string title = comp.Pawn.LabelShortCap + " — " + comp.classDef.LabelCap;
            if (comp.subclassDef != null) title += " · " + comp.subclassDef.LabelCap;
            Widgets.Label(new Rect(icon.xMax + 12f, r.y + 6f, r.width - 500f, 32f), title.Colorize(cls.color));
            Text.Font = GameFont.Small;
            var lvlRect = new Rect(icon.xMax + 12f, r.y + 38f, 420f, 22f);
            string lvl = "AA_LevelLong".Translate(comp.level, CompArcane.MaxLevel);
            if (comp.paragon > 0) lvl += "  " + "AA_ParagonLabel".Translate(comp.paragon).Colorize(Gold);
            Widgets.Label(lvlRect, lvl);
            var xpRect = new Rect(icon.xMax + 12f, r.y + 60f, 420f, 8f);
            Widgets.FillableBar(xpRect, Mathf.Clamp01(comp.xp / comp.XpToNextLevel), ArcaneTex.XpFill, ArcaneTex.BarBg, false);
            TooltipHandler.TipRegion(xpRect.ExpandedBy(0f, 6f), "AA_XpTip".Translate(comp.xp.ToString("F0"), comp.XpToNextLevel.ToString("F0")));

            float bx = r.xMax - 470f;
            DrawBadge(new Rect(bx, r.y + 10f, 150f, 54f), "AA_PerkPoints".Translate(), comp.perkPoints.ToString(), comp.perkPoints > 0 ? Gold : Color.gray);
            DrawBadge(new Rect(bx + 158f, r.y + 10f, 150f, 54f), "AA_AttributePoints".Translate(), comp.attributePoints.ToString(), comp.attributePoints > 0 ? Gold : Color.gray);
            var manaRect = new Rect(bx + 316f, r.y + 14f, 150f, 20f);
            Widgets.FillableBar(manaRect, comp.mana / Mathf.Max(1f, comp.ManaMax), ArcaneTex.ManaFill, ArcaneTex.ManaBg, false);
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(manaRect, "AA_ManaWord".Translate() + " " + comp.mana.ToString("F0") + "/" + comp.ManaMax.ToString("F0"));
            if (comp.WardMax > 0.5f)
            {
                var wardRect = new Rect(bx + 316f, r.y + 38f, 150f, 16f);
                Widgets.FillableBar(wardRect, comp.ward / comp.WardMax, ArcaneTex.WardFill, ArcaneTex.BarBg, false);
                Widgets.Label(wardRect, "AA_WardWord".Translate() + " " + comp.ward.ToString("F0") + "/" + comp.WardMax.ToString("F0"));
            }
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
        }

        private void DrawBadge(Rect r, string label, string value, Color c)
        {
            Widgets.DrawBoxSolid(r, new Color(0.08f, 0.07f, 0.12f));
            Widgets.DrawBoxSolidWithOutline(r, Color.clear, c * new Color(1, 1, 1, 0.6f));
            Text.Anchor = TextAnchor.UpperCenter;
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(r.x, r.y + 3f, r.width, 16f), label);
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(r.x, r.y + 18f, r.width, 32f), value.Colorize(c));
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DrawTabs(Rect r)
        {
            var tabs = new List<(Tab, string)>
            {
                (Tab.ClassTree, comp.classDef.LabelCap + " " + "AA_TreeWord".Translate()),
                (Tab.SubclassTree, comp.subclassDef != null ? comp.subclassDef.LabelCap.ToString() : (string)"AA_SubclassTab".Translate()),
                (Tab.Spellbook, "AA_SpellbookTab".Translate()),
                (Tab.Codex, "AA_CodexTab".Translate()),
            };
            float w = Mathf.Min(220f, r.width / tabs.Count - 6f);
            for (int i = 0; i < tabs.Count; i++)
            {
                var (t, label) = tabs[i];
                var tr = new Rect(r.x + i * (w + 6f), r.y, w, r.height);
                bool sel = tab == t;
                Widgets.DrawBoxSolid(tr, sel ? new Color(0.14f, 0.11f, 0.22f) : new Color(0.07f, 0.06f, 0.1f));
                Widgets.DrawBoxSolidWithOutline(tr, Color.clear, sel ? comp.DisplayClass.color : new Color(0.3f, 0.3f, 0.35f));
                Text.Anchor = TextAnchor.MiddleCenter;
                string l = label;
                if (t == Tab.SubclassTree && comp.CanChooseSubclass)
                    l = l.Colorize(Color.Lerp(Gold, Color.white, 0.5f + 0.5f * Mathf.Sin(Time.realtimeSinceStartup * 4f)));
                Widgets.Label(tr, l);
                Text.Anchor = TextAnchor.UpperLeft;
                if (Widgets.ButtonInvisible(tr))
                {
                    tab = t;
                    previewSubclass = null;
                    SoundDefOf.Tick_High.PlayOneShotOnCamera();
                }
            }
        }

        #endregion

        #region Sidebar

        private void DrawSidebar(Rect r)
        {
            Widgets.DrawBoxSolid(r, PanelBg);
            Widgets.DrawBoxSolidWithOutline(r, Color.clear, new Color(0.25f, 0.22f, 0.35f));
            var inner = r.ContractedBy(8f);
            var view = new Rect(0f, 0f, inner.width - 16f, 760f);
            Widgets.BeginScrollView(inner, ref sideScroll, view);
            float y = 0f;
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(0f, y, view.width, 24f), "AA_Attributes".Translate().Colorize(Gold));
            y += 26f;
            foreach (var attr in DefDatabase<ArcaneAttributeDef>.AllDefsListForReading.OrderBy(a => a.order))
            {
                var row = new Rect(0f, y, view.width, 48f);
                int v = comp.Attribute(attr);
                Widgets.DrawBoxSolid(row, new Color(0.07f, 0.06f, 0.1f));
                GUI.DrawTexture(new Rect(row.x + 4f, row.y + 6f, 36f, 36f), attr.Icon);
                Widgets.Label(new Rect(row.x + 46f, row.y + 2f, row.width - 90f, 22f), attr.LabelCap.Colorize(attr.color));
                var pipRect = new Rect(row.x + 46f, row.y + 26f, row.width - 92f, 10f);
                float frac = v / (float)CompArcane.AttributeMax;
                Widgets.FillableBar(pipRect, frac, BarTex(attr.color), ArcaneTex.BarBg, false);
                Text.Anchor = TextAnchor.MiddleRight;
                Text.Font = GameFont.Medium;
                Widgets.Label(new Rect(row.xMax - 74f, row.y, 40f, row.height), v.ToString());
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperLeft;
                var plus = new Rect(row.xMax - 30f, row.y + 11f, 26f, 26f);
                bool can = comp.CanRaiseAttribute(attr);
                if (can)
                {
                    if (Widgets.ButtonText(plus, "+"))
                    {
                        comp.RaiseAttribute(attr);
                        ArcaneDefOf.AA_NodeUnlock?.PlayOneShotOnCamera();
                    }
                }
                TooltipHandler.TipRegion(row, () => AttributeTip(attr), attr.GetHashCode());
                y += 52f;
            }
            y += 8f;
            Widgets.Label(new Rect(0f, y, view.width, 24f), "AA_ArcaneStats".Translate().Colorize(Gold));
            y += 26f;
            var p = comp.Pawn;
            Text.Font = GameFont.Tiny;
            void Stat(string label, string val)
            {
                Widgets.Label(new Rect(0f, y, view.width * 0.6f, 18f), label);
                Text.Anchor = TextAnchor.UpperRight;
                Widgets.Label(new Rect(0f, y, view.width, 18f), val);
                Text.Anchor = TextAnchor.UpperLeft;
                y += 18f;
            }
            Stat("AA_SpellPowerShort".Translate(), comp.SpellPower.ToStringPercent());
            Stat(ArcaneDefOf.AA_ManaMax.LabelCap, comp.ManaMax.ToString("F0"));
            Stat(ArcaneDefOf.AA_ManaRegen.LabelCap, comp.ManaRegenPerSecond.ToString("F2") + "/s");
            Stat(ArcaneDefOf.AA_CooldownReduction.LabelCap, Mathf.Clamp(p.GetStatValue(ArcaneDefOf.AA_CooldownReduction), 0f, 0.75f).ToStringPercent());
            Stat(ArcaneDefOf.AA_CastSpeed.LabelCap, p.GetStatValue(ArcaneDefOf.AA_CastSpeed).ToStringPercent());
            Stat(ArcaneDefOf.AA_CritChance.LabelCap, p.GetStatValue(ArcaneDefOf.AA_CritChance).ToStringPercent());
            Stat(ArcaneDefOf.AA_CritDamage.LabelCap, p.GetStatValue(ArcaneDefOf.AA_CritDamage).ToStringPercent());
            Stat(ArcaneDefOf.AA_ManaCostFactor.LabelCap, p.GetStatValue(ArcaneDefOf.AA_ManaCostFactor).ToStringPercent());
            Stat(ArcaneDefOf.AA_WardMax.LabelCap, comp.WardMax.ToString("F0"));
            Stat(ArcaneDefOf.AA_AreaFactor.LabelCap, p.GetStatValue(ArcaneDefOf.AA_AreaFactor).ToStringPercent());
            Stat(ArcaneDefOf.AA_DurationFactor.LabelCap, p.GetStatValue(ArcaneDefOf.AA_DurationFactor).ToStringPercent());
            Stat(ArcaneDefOf.AA_SummonPower.LabelCap, p.GetStatValue(ArcaneDefOf.AA_SummonPower).ToStringPercent());
            Stat(ArcaneDefOf.AA_HealingPower.LabelCap, p.GetStatValue(ArcaneDefOf.AA_HealingPower).ToStringPercent());
            Stat(ArcaneDefOf.AA_Lifesteal.LabelCap, p.GetStatValue(ArcaneDefOf.AA_Lifesteal).ToStringPercent());
            Stat(ArcaneDefOf.AA_XpGain.LabelCap, p.GetStatValue(ArcaneDefOf.AA_XpGain).ToStringPercent());
            Text.Font = GameFont.Small;
            y += 10f;
            if (Widgets.ButtonText(new Rect(0f, y, view.width, 28f), "AA_AutoAllocate".Translate()))
            {
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("AA_AutoAllocateConfirm".Translate(), () => ArcaneUtility.AutoAllocate(comp)));
            }
            y += 32f;
            Widgets.EndScrollView();
        }

        private string AttributeTip(ArcaneAttributeDef attr)
        {
            var lines = new List<string> { attr.LabelCap.Colorize(attr.color), attr.description, "", "AA_PerPoint".Translate() };
            foreach (var sm in attr.statOffsetsPerPoint)
                lines.Add("• " + sm.stat.LabelCap + ": " + sm.stat.Worker.ValueToString(sm.value, false, ToStringNumberSense.Offset));
            lines.Add("");
            lines.Add("AA_AttrGates".Translate());
            return string.Join("\n", lines);
        }

        #endregion

        #region Trees

        private Vector2 NodeCenter(ArcaneNodeDef n) => new Vector2(TreeMargin + n.x * CellW * zoom, TreeMargin + n.y * CellH * zoom);

        private void DrawTree(Rect r, ArcaneClassDef tree, bool interactive)
        {
            Widgets.DrawBoxSolid(r, new Color(0.02f, 0.018f, 0.035f));
            GUI.color = tree.color * new Color(1, 1, 1, 0.07f);
            float bgSize = Mathf.Min(r.width, r.height) * 0.95f;
            Widgets.DrawTextureRotated(new Rect(r.center.x - bgSize / 2f, r.center.y - bgSize / 2f, bgSize, bgSize), ArcaneTex.Rune, Time.realtimeSinceStartup * 2f);
            GUI.color = Color.white;
            Widgets.DrawBoxSolidWithOutline(r, Color.clear, tree.color * new Color(1, 1, 1, 0.35f));

            var zoomRect = new Rect(r.xMax - 96f, r.y + 6f, 88f, 24f);
            if (Widgets.ButtonText(new Rect(zoomRect.x, zoomRect.y, 40f, 24f), "−")) zoom = Mathf.Max(0.6f, zoom - 0.1f);
            if (Widgets.ButtonText(new Rect(zoomRect.x + 46f, zoomRect.y, 40f, 24f), "+")) zoom = Mathf.Min(1.4f, zoom + 0.1f);

            float contentW = TreeMargin * 2 + (tree.treeWidth - 1) * CellW * zoom;
            float contentH = TreeMargin * 2 + (tree.treeHeight - 1) * CellH * zoom;
            var outRect = r.ContractedBy(2f);
            var view = new Rect(0f, 0f, Mathf.Max(contentW, outRect.width - 16f), Mathf.Max(contentH, outRect.height - 16f));
            float offX = Mathf.Max(0f, (view.width - contentW) / 2f);
            Widgets.BeginScrollView(outRect, ref treeScroll, view);
            var origin = new Vector2(offX, 0f);

            // tier labels
            Text.Font = GameFont.Tiny;
            var tiers = tree.nodes.GroupBy(n => n.y).OrderBy(g => g.Key);
            foreach (var g in tiers)
            {
                var gate = g.Where(n => n.requiredAttribute != null).OrderBy(n => n.requiredAttributeValue).FirstOrDefault();
                int lvlReq = g.Min(n => n.requiredLevel);
                string label = gate != null ? gate.requiredAttribute.abbreviation + " " + gate.requiredAttributeValue : lvlReq > 1 ? "AA_LevelShort".Translate(lvlReq).ToString() : "";
                if (label.NullOrEmpty()) continue;
                float yy = origin.y + TreeMargin + g.Key * CellH * zoom;
                GUI.color = new Color(1f, 1f, 1f, 0.06f);
                Widgets.DrawLineHorizontal(0f, yy, view.width);
                GUI.color = ColoredText.SubtleGrayColor;
                Widgets.Label(new Rect(4f, yy - 9f, 70f, 18f), label);
                GUI.color = Color.white;
            }
            Text.Font = GameFont.Small;

            // connections
            foreach (var n in tree.nodes)
                foreach (var pre in n.prerequisites)
                {
                    if (pre.tree != tree) continue;
                    Vector2 a = origin + NodeCenter(pre);
                    Vector2 b = origin + NodeCenter(n);
                    bool lit = comp.Rank(pre) > 0 && comp.Rank(n) > 0;
                    bool avail = comp.Rank(pre) > 0;
                    Color c = lit ? tree.color : avail ? tree.color * new Color(0.6f, 0.6f, 0.6f, 0.7f) : new Color(0.25f, 0.25f, 0.3f, 0.6f);
                    if (lit) Widgets.DrawLine(a, b, tree.color * new Color(1, 1, 1, 0.25f), 9f * zoom);
                    Widgets.DrawLine(a, b, c, (lit ? 3.5f : 2f) * zoom);
                    if (lit)
                    {
                        float t = (Time.realtimeSinceStartup * 0.6f + (n.x + n.y) * 0.13f) % 1f;
                        var mid = Vector2.Lerp(a, b, t);
                        GUI.color = Color.Lerp(tree.color, Color.white, 0.6f);
                        GUI.DrawTexture(new Rect(mid.x - 5f, mid.y - 5f, 10f, 10f), ArcaneTex.NodeGlow);
                        GUI.color = Color.white;
                    }
                }

            foreach (var n in tree.nodes) DrawNode(origin + NodeCenter(n), n, tree, interactive);
            Widgets.EndScrollView();
        }

        private bool DrawNode(Vector2 center, ArcaneNodeDef n, ArcaneClassDef tree, bool interactive)
        {
            float size;
            Texture2D frame;
            switch (n.kind)
            {
                case NodeKind.Spell: size = 60f; frame = ArcaneTex.NodeFrameSpell; break;
                case NodeKind.Keystone: size = 66f; frame = ArcaneTex.NodeFrameKeystone; break;
                case NodeKind.Ultimate: size = 76f; frame = ArcaneTex.NodeFrameKeystone; break;
                case NodeKind.Notable: size = 54f; frame = ArcaneTex.NodeFrame; break;
                case NodeKind.Augment: size = 46f; frame = ArcaneTex.NodeFrameSpell; break;
                default: size = 44f; frame = ArcaneTex.NodeFrame; break;
            }
            size *= zoom;
            var rect = new Rect(center.x - size / 2f, center.y - size / 2f, size, size);
            int rank = comp.Rank(n);
            var can = interactive ? comp.CanRankUp(n) : (AcceptanceReport)false;
            bool learned = rank > 0;
            bool available = can.Accepted;
            float t = Time.realtimeSinceStartup;
            Color col = tree.color;

            if (learned || available || n.kind == NodeKind.Ultimate)
            {
                float a = learned ? 0.55f : available ? 0.25f + 0.25f * Mathf.Sin(t * 3.5f + n.x) : 0.12f;
                GUI.color = col * new Color(1, 1, 1, a);
                float g = n.kind == NodeKind.Ultimate || n.kind == NodeKind.Keystone ? 2.1f : 1.75f;
                GUI.DrawTexture(new Rect(center.x - size * g / 2f, center.y - size * g / 2f, size * g, size * g), ArcaneTex.NodeGlow);
            }
            GUI.color = new Color(0.04f, 0.035f, 0.06f, 0.95f);
            GUI.DrawTexture(rect.ContractedBy(size * 0.08f), ArcaneTex.NodeGlow);
            GUI.color = learned ? Color.white : available ? new Color(0.85f, 0.85f, 0.85f) : new Color(0.38f, 0.38f, 0.42f);
            GUI.DrawTexture(rect.ContractedBy(size * 0.2f), n.Icon);
            GUI.color = learned ? col : available ? Color.Lerp(col, Color.white, 0.3f) * new Color(1, 1, 1, 0.8f) : new Color(0.35f, 0.35f, 0.4f);
            GUI.DrawTexture(rect, frame);
            GUI.color = Color.white;

            if (n.maxRank > 1)
            {
                float pip = 7f * zoom;
                float total = n.maxRank * (pip + 2f);
                float px = center.x - total / 2f;
                for (int i = 0; i < n.maxRank; i++)
                {
                    var pr = new Rect(px + i * (pip + 2f), rect.yMax + 2f, pip, pip);
                    Widgets.DrawBoxSolid(pr, i < rank ? col : new Color(0.2f, 0.2f, 0.25f));
                }
            }
            if (zoom >= 0.85f)
            {
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.UpperCenter;
                var lr = new Rect(center.x - 60f, rect.yMax + (n.maxRank > 1 ? 10f : 2f), 120f, 30f);
                GUI.color = learned ? Color.white : available ? new Color(0.85f, 0.85f, 0.85f) : new Color(0.5f, 0.5f, 0.55f);
                Widgets.Label(lr, n.LabelCap);
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Small;
            }

            bool over = Mouse.IsOver(rect);
            if (over)
            {
                GUI.color = new Color(1, 1, 1, 0.25f);
                GUI.DrawTexture(rect.ExpandedBy(4f), ArcaneTex.NodeGlow);
                GUI.color = Color.white;
                TooltipHandler.TipRegion(rect, new TipSignal(() => ArcaneUtility.NodeTooltip(n, interactive ? comp : null), n.shortHash));
            }
            if (interactive && Widgets.ButtonInvisible(rect))
            {
                if (available)
                {
                    comp.RankUp(n);
                    (ArcaneDefOf.AA_NodeUnlock ?? SoundDefOf.Tick_High).PlayOneShotOnCamera();
                }
                else
                {
                    SoundDefOf.ClickReject.PlayOneShotOnCamera();
                    if (!can.Reason.NullOrEmpty()) Messages.Message(can.Reason, MessageTypeDefOf.RejectInput, false);
                }
            }
            return over;
        }

        private void DrawTreePreview(Rect r, ArcaneClassDef sub)
        {
            var top = new Rect(r.x, r.y, r.width, 34f);
            Widgets.Label(new Rect(top.x + 6f, top.y + 6f, 600f, 26f), "AA_PreviewingTree".Translate(sub.label).Colorize(sub.color));
            if (Widgets.ButtonText(new Rect(top.xMax - 160f, top.y + 2f, 150f, 28f), "AA_BackToChoices".Translate())) previewSubclass = null;
            DrawTree(new Rect(r.x, r.y + 36f, r.width, r.height - 36f), sub, false);
        }

        private void DrawSubclassChoice(Rect r)
        {
            Widgets.DrawBoxSolid(r, new Color(0.02f, 0.018f, 0.035f));
            var subs = comp.classDef.subclasses;
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.UpperCenter;
            string head = comp.CanChooseSubclass ? "AA_ChooseSubclassNow".Translate() : "AA_SubclassLocked".Translate(comp.classDef.subclassUnlockLevel);
            Widgets.Label(new Rect(r.x, r.y + 8f, r.width, 34f), head);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            if (subs.Count == 0) return;
            float w = (r.width - 40f - (subs.Count - 1) * 14f) / subs.Count;
            for (int i = 0; i < subs.Count; i++)
            {
                var sub = subs[i];
                var card = new Rect(r.x + 20f + i * (w + 14f), r.y + 52f, w, r.height - 70f);
                Widgets.DrawBoxSolid(card, new Color(0.06f, 0.05f, 0.09f));
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.realtimeSinceStartup * 2f + i);
                Widgets.DrawBoxSolidWithOutline(card, Color.clear, sub.color * new Color(1, 1, 1, 0.4f + 0.3f * pulse), 2);
                var inner = card.ContractedBy(12f);
                var icon = new Rect(inner.center.x - 48f, inner.y, 96f, 96f);
                GUI.color = sub.color * new Color(1, 1, 1, 0.4f);
                GUI.DrawTexture(icon.ExpandedBy(16f), ArcaneTex.NodeGlow);
                GUI.color = Color.white;
                GUI.DrawTexture(icon, sub.Icon);
                float y = icon.yMax + 8f;
                Text.Font = GameFont.Medium;
                Text.Anchor = TextAnchor.UpperCenter;
                Widgets.Label(new Rect(inner.x, y, inner.width, 34f), sub.LabelCap.Colorize(sub.color));
                Text.Font = GameFont.Small;
                y += 34f;
                GUI.color = ColoredText.SubtleGrayColor;
                float th = Text.CalcHeight(sub.tagline, inner.width);
                Widgets.Label(new Rect(inner.x, y, inner.width, th), sub.tagline);
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                y += th + 8f;
                Text.Font = GameFont.Tiny;
                string body = sub.description + (sub.playstyle.NullOrEmpty() ? "" : "\n\n" + sub.playstyle);
                float bh = Mathf.Min(Text.CalcHeight(body, inner.width), inner.yMax - y - 120f);
                Widgets.Label(new Rect(inner.x, y, inner.width, bh), body);
                Text.Font = GameFont.Small;
                y += bh + 8f;
                float sx = inner.x;
                foreach (var spell in sub.AllSpells.Take(8))
                {
                    var sr = new Rect(sx, y, 34f, 34f);
                    GUI.DrawTexture(sr, spell.uiIcon);
                    TooltipHandler.TipRegion(sr, () => ArcaneUtility.SpellTooltip(spell, comp.Pawn), spell.GetHashCode() + 7);
                    sx += 38f;
                    if (sx > inner.xMax - 34f) break;
                }
                var b1 = new Rect(inner.x, inner.yMax - 66f, inner.width, 28f);
                var b2 = new Rect(inner.x, inner.yMax - 32f, inner.width, 32f);
                if (Widgets.ButtonText(b1, "AA_PreviewTree".Translate())) previewSubclass = sub;
                if (comp.CanChooseSubclass)
                {
                    if (Widgets.ButtonText(b2, "AA_BecomeSubclass".Translate(sub.label)))
                    {
                        var s = sub;
                        Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("AA_ConfirmSubclass".Translate(s.label, comp.Pawn.LabelShort), () =>
                        {
                            comp.ChooseSubclass(s);
                            tab = Tab.SubclassTree;
                        }, true));
                    }
                }
                else
                {
                    GUI.color = Color.gray;
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label(b2, "AA_ReachLevel".Translate(comp.classDef.subclassUnlockLevel));
                    Text.Anchor = TextAnchor.UpperLeft;
                    GUI.color = Color.white;
                }
            }
        }

        #endregion

        #region Spellbook & codex

        private void DrawSpellbook(Rect r)
        {
            Widgets.DrawBoxSolid(r, new Color(0.03f, 0.025f, 0.05f));
            var known = comp.KnownSpells().ToList();
            var all = comp.classDef.AllSpells.ToList();
            if (comp.subclassDef != null) all.AddRange(comp.subclassDef.AllSpells);
            var unknown = all.Where(s => !known.Contains(s)).Distinct().ToList();
            float rowH = 64f;
            var view = new Rect(0f, 0f, r.width - 20f, 40f + (known.Count + unknown.Count + 2) * (rowH + 4f));
            Widgets.BeginScrollView(r.ContractedBy(4f), ref bookScroll, view);
            float y = 0f;
            Widgets.Label(new Rect(6f, y, view.width, 26f), "AA_KnownSpells".Translate(known.Count).Colorize(Gold));
            y += 30f;
            foreach (var s in known)
            {
                DrawSpellRow(new Rect(0f, y, view.width, rowH), s, true);
                y += rowH + 4f;
            }
            y += 10f;
            Widgets.Label(new Rect(6f, y, view.width, 26f), "AA_LockedSpells".Translate(unknown.Count).Colorize(Color.gray));
            y += 30f;
            foreach (var s in unknown)
            {
                DrawSpellRow(new Rect(0f, y, view.width, rowH), s, false);
                y += rowH + 4f;
            }
            Widgets.EndScrollView();
        }

        private void DrawSpellRow(Rect r, AbilityDef s, bool known)
        {
            var ext = s.GetModExtension<SpellExtension>();
            var props = s.comps?.OfType<CompProperties_ArcaneSpell>().FirstOrDefault();
            Color col = props?.color ?? Color.white;
            Widgets.DrawBoxSolid(r, new Color(0.07f, 0.06f, 0.1f));
            if (known) Widgets.DrawBoxSolidWithOutline(r, Color.clear, col * new Color(1, 1, 1, 0.3f));
            var icon = new Rect(r.x + 6f, r.y + 6f, 52f, 52f);
            if (!known) GUI.color = new Color(0.4f, 0.4f, 0.45f);
            GUI.DrawTexture(icon, s.uiIcon);
            GUI.color = Color.white;
            Widgets.Label(new Rect(icon.xMax + 10f, r.y + 4f, 360f, 24f), (s.LabelCap + (ext?.ultimate ?? false ? "  ★" : "")).Colorize(known ? col : Color.gray));
            Text.Font = GameFont.Tiny;
            GUI.color = ColoredText.SubtleGrayColor;
            string meta = "AA_SpellMeta".Translate(ext?.manaCost.ToString("F0") ?? "0", s.cooldownTicksRange.max.ToStringTicksToPeriod(), ("AA_Role_" + (ext?.role ?? SpellRole.Attack)).Translate());
            if (ext != null && ext.tags.Count > 0) meta += " · " + string.Join(", ", ext.tags);
            Widgets.Label(new Rect(icon.xMax + 10f, r.y + 26f, r.width - 400f, 18f), meta);
            GUI.color = Color.white;
            string desc = s.description ?? "";
            Widgets.Label(new Rect(icon.xMax + 10f, r.y + 42f, r.width - icon.width - 340f, 20f), desc.Truncate(r.width - icon.width - 340f));
            Text.Font = GameFont.Small;
            TooltipHandler.TipRegion(r, () => ArcaneUtility.SpellTooltip(s, comp.Pawn), s.GetHashCode() + 13);
            if (known)
            {
                bool auto = comp.IsAutocast(s);
                bool shown = !comp.IsHidden(s);
                var a1 = new Rect(r.xMax - 300f, r.y + 18f, 140f, 28f);
                var a2 = new Rect(r.xMax - 150f, r.y + 18f, 140f, 28f);
                bool autoBefore = auto, shownBefore = shown;
                bool canAuto = ext != null && ext.role != SpellRole.Utility && ext.role != SpellRole.Mobility;
                if (canAuto) Widgets.CheckboxLabeled(a1, "AA_Autocast".Translate(), ref auto);
                Widgets.CheckboxLabeled(a2, "AA_ShowOnBar".Translate(), ref shown);
                if (auto != autoBefore) comp.SetAutocast(s, auto);
                if (shown != shownBefore) comp.SetHidden(s, !shown);
            }
        }

        private void DrawCodex(Rect r)
        {
            Widgets.DrawBoxSolid(r, new Color(0.03f, 0.025f, 0.05f));
            var inner = r.ContractedBy(14f);
            var view = new Rect(0f, 0f, inner.width - 16f, 2000f);
            Widgets.BeginScrollView(inner, ref codexScroll, view);
            float y = 0f;
            void Section(ArcaneClassDef c)
            {
                GUI.DrawTexture(new Rect(0f, y, 56f, 56f), c.Icon);
                Text.Font = GameFont.Medium;
                Widgets.Label(new Rect(66f, y + 10f, view.width - 66f, 34f), c.LabelCap.Colorize(c.color));
                Text.Font = GameFont.Small;
                y += 62f;
                foreach (var text in new[] { c.description, c.playstyle, c.lore })
                {
                    if (text.NullOrEmpty()) continue;
                    float h = Text.CalcHeight(text, view.width);
                    Widgets.Label(new Rect(0f, y, view.width, h), text);
                    y += h + 8f;
                }
                y += 10f;
            }
            Section(comp.classDef);
            if (comp.subclassDef != null) Section(comp.subclassDef);
            Widgets.Label(new Rect(0f, y, view.width, 24f), "AA_CodexTips".Translate().Colorize(Gold));
            y += 26f;
            string tips = "AA_CodexTipsBody".Translate();
            float th = Text.CalcHeight(tips, view.width);
            Widgets.Label(new Rect(0f, y, view.width, th), tips);
            Widgets.EndScrollView();
        }

        #endregion
    }
}
