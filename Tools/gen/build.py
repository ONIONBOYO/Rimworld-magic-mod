"""Build all generated defs + icon manifest. Usage: python3 Tools/gen/build.py"""
import importlib
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
sys.path.insert(0, HERE)

import arcgen  # noqa: E402
from arcgen import write_defs, write_json, node_xml  # noqa: E402
import core_content  # noqa: E402
import things  # noqa: E402
import sounds_flecks  # noqa: E402
import systems  # noqa: E402
from classes import expansion  # noqa: E402
from classes import expansion2  # noqa: E402

CLASS_MODULES = [
    "c_pyromancer", "c_tempest", "c_necromancer", "c_paladin", "c_chronomancer",
    "c_druid", "c_runesmith", "c_shadow", "c_bard",
    "c_harbinger", "c_windmage", "c_succubus", "c_incubus", "c_noclipper",
]

STAT_MOTIF = {
    "AA_SpellPower": "burst", "AA_ManaMax": "orb", "AA_ManaRegen": "drop", "AA_CooldownReduction": "hourglass",
    "AA_CastSpeed": "swirl", "AA_CritChance": "eye", "AA_CritDamage": "star", "AA_WardMax": "shield", "AA_WardRegen": "shield",
    "AA_AreaFactor": "ring", "AA_DurationFactor": "clock", "AA_SummonPower": "hand", "AA_HealingPower": "heart",
    "AA_Lifesteal": "drop", "AA_XpGain": "book", "ArmorRating_Sharp": "shield", "ArmorRating_Blunt": "shield",
    "ArmorRating_Heat": "shield", "MoveSpeed": "wing", "AA_ManaCostFactor": "orb",
}


def node_motif(n, spells_by_id):
    if n.icon:
        return n.icon
    for k in list(n.stats) + list(n.pct):
        if k in STAT_MOTIF:
            return STAT_MOTIF[k]
    for m in n.mods:
        sp = m.fields.get("spells")
        if sp:
            sid = sp[0].replace("AA_Spell_", "")
            if sid in spells_by_id:
                ic = spells_by_id[sid].icon
                return ic if isinstance(ic, str) else ic[0]
    return "rune"


def colony_extension(modname):
    """The colony expansion of a class lives in classes/x_<name>.py (x_pyromancer for c_pyromancer) and adds spells, nodes
    and extra defs to the trees through extend(trees_by_cid, extra). Missing modules are fine."""
    name = "classes.x_" + modname[2:]
    try:
        return importlib.import_module(name)
    except ModuleNotFoundError as e:
        if e.name == name:
            return None
        raise


def load_class(modname):
    mod = importlib.import_module("classes." + modname)
    things.THING_ICONS.clear()
    root, subs, extra = mod.build()
    keys = dict(getattr(mod, "KEYS", {}))
    expansion.extend(modname, root, subs, extra)
    expansion2.extend(modname, root, subs, extra)
    ext = colony_extension(modname)
    if ext is not None:
        ext.extend({t.cid: t for t in [root] + subs}, extra)
        keys.update(getattr(ext, "KEYS", {}))
    return root, subs, extra, keys


def main():
    import argparse
    ap = argparse.ArgumentParser()
    ap.add_argument("--dry", action="store_true", help="validate everything but write nothing")
    ap.add_argument("--only", action="append", help="only build these class modules (implies --dry); spell ids are still checked against the rest")
    ap.add_argument("--emit", help="write the def XML to this folder instead of Defs/Generated (nothing else is written); with --only, other "
                                   "classes are still emitted when they build cleanly, so cross-class references resolve")
    args = ap.parse_args()
    emit = args.emit
    dry = (args.dry or bool(args.only)) and not emit
    only = set(args.only or [])
    if only:
        for m in only:
            if m not in CLASS_MODULES:
                raise SystemExit("unknown class module %s (expected one of %s)" % (m, ", ".join(CLASS_MODULES)))

    out_defs = os.path.abspath(emit) if emit else os.path.join(ROOT, "Defs", "Generated")
    if not dry:
        os.makedirs(out_defs, exist_ok=True)
        for f in os.listdir(out_defs):
            if f.endswith(".xml"):
                os.remove(os.path.join(out_defs, f))
    else:
        globals()["write_defs"] = lambda path, chunks: None

    write_defs(os.path.join(out_defs, "Core_Damage.xml"), core_content.damage_xml())
    write_defs(os.path.join(out_defs, "Core_Status.xml"), core_content.status_xml())
    write_defs(os.path.join(out_defs, "Core_Sounds.xml"), sounds_flecks.sound_xml())
    write_defs(os.path.join(out_defs, "Core_Flecks.xml"), sounds_flecks.fleck_xml())
    write_defs(os.path.join(out_defs, "Core_Systems.xml"), systems.strain_xml() + systems.reactions_xml() + systems.familiars_xml())

    icons = []
    compendium = []
    keyed = {}
    all_spell_ids = {}
    all_def_names = set()
    stats = {"classes": 0, "subclasses": 0, "spells": 0, "nodes": 0}
    skip = set()
    if only:
        # other classes are only loaded for their spell ids, so a half-finished module elsewhere doesn't block this check
        for modname in CLASS_MODULES:
            if modname in only:
                continue
            try:
                r, ss, _, _ = load_class(modname)
            except Exception as e:  # noqa: BLE001
                print("  (could not load %s for the id check: %s)" % (modname, e))
                skip.add(modname)
                continue
            for t in [r] + ss:
                for sp in t.spells:
                    all_spell_ids[sp.sid] = modname
    for modname in CLASS_MODULES:
        if only and modname not in only and not emit:
            continue
        if modname in skip:
            continue
        if only and modname not in only:
            # emitted only so references resolve: ids were registered above, and a broken module elsewhere is not this check's problem
            try:
                root, subs, extra, keys = load_class(modname)
                fname = "Class_%02d_%s.xml" % (CLASS_MODULES.index(modname) + 1, root.cid)
                chunks = [t.xml() for t in [root] + subs]
                for t in [root] + subs:
                    by_id = {n.nid: n for n in t.nodes}
                    chunks += [node_xml(t, n, by_id) for n in t.nodes]
                    chunks += [sp.xml() for sp in t.spells]
                chunks.extend(extra)
                write_defs(os.path.join(out_defs, fname), chunks)
            except Exception as e:  # noqa: BLE001
                print("  (not emitting %s: %s)" % (modname, e))
            continue
        try:
            root, subs, extra, keys = load_class(modname)
        except ModuleNotFoundError as e:
            if e.name == "classes." + modname:
                print("  (skipping missing module %s)" % modname)
                continue
            raise
        keyed.update(keys)
        trees = [root] + subs
        compendium.append(trees)
        spells_by_id = {}
        chunks = []
        for t in trees:
            chunks.append(t.xml())
            for s in t.spells:
                if s.sid in spells_by_id:
                    raise ValueError("duplicate spell " + s.sid)
                if all_spell_ids.get(s.sid, modname) != modname:
                    raise ValueError("spell %s is also defined by %s" % (s.sid, all_spell_ids[s.sid]))
                all_spell_ids[s.sid] = modname
                spells_by_id[s.sid] = s
        # validation
        used = set()
        for t in trees:
            for s in t.starting:
                if s not in spells_by_id:
                    raise KeyError("starting spell %s missing in %s" % (s, t.cid))
                used.add(s)
            by_id = {n.nid: n for n in t.nodes}
            if len(by_id) != len(t.nodes):
                raise ValueError("duplicate node id in " + t.cid)
            pos = {}
            for n in t.nodes:
                if (n.x, n.y) in pos:
                    raise ValueError("overlapping nodes %s and %s in %s" % (n.nid, pos[(n.x, n.y)], t.cid))
                pos[(n.x, n.y)] = n.nid
                if not (0 <= n.x < t.width and 0 <= n.y < t.height):
                    raise ValueError("node %s out of bounds in %s" % (n.nid, t.cid))
                if n.spell:
                    if n.spell not in spells_by_id:
                        raise KeyError("node %s references unknown spell %s" % (n.nid, n.spell))
                    if n.spell in used:
                        raise ValueError("spell %s unlocked twice" % n.spell)
                    used.add(n.spell)
                for p in n.pre:
                    if p in by_id and by_id[p].y >= n.y:
                        raise ValueError("prerequisite %s of %s is not above it in %s" % (p, n.nid, t.cid))
                chunks.append(node_xml(t, n, by_id))
                stats["nodes"] += 1
                if not n.spell:
                    icons.append({"path": "ArcanumAscendant/Nodes/%s_%s" % (t.cid, n.nid), "kind": "node", "motif": node_motif(n, spells_by_id),
                                  "c1": t.hex1, "c2": t.hex2, "frame": n.kind})
            ci = t.icon if isinstance(t.icon, (list, tuple)) else (t.icon,)
            icons.append({"path": "ArcanumAscendant/Classes/" + t.cid, "kind": "class", "motif": ci[0], "motif2": ci[1] if len(ci) > 1 else None,
                          "c1": t.hex1, "c2": t.hex2, "frame": "class" if not t.parent else "subclass"})
            stats["subclasses" if t.parent else "classes"] += 1
        unused = set(spells_by_id) - used
        if unused:
            raise ValueError("spells never unlocked: %s" % sorted(unused))
        for t in trees:
            for s in t.spells:
                chunks.append(s.xml())
                ic = s.icon if isinstance(s.icon, (list, tuple)) else (s.icon,)
                icons.append({"path": "ArcanumAscendant/Spells/" + s.sid, "kind": "spell", "motif": ic[0], "motif2": ic[1] if len(ic) > 1 else None,
                              "c1": s.school.hex1, "c2": s.school.hex2, "frame": "ultimate" if s.ultimate else "spell"})
                stats["spells"] += 1
        chunks.extend(extra)
        for path, kind, c1, c2 in things.THING_ICONS:
            icons.append({"path": path, "kind": "thing", "motif": kind, "c1": c1, "c2": c2, "frame": "thing"})
        fname = "Class_%02d_%s.xml" % (CLASS_MODULES.index(modname) + 1, root.cid)
        write_defs(os.path.join(out_defs, fname), chunks)
        for c in chunks:
            # a race and its pawn kind may share a name; two defs of the same type may not
            m = re.match(r"\s*<([A-Za-z0-9_.]+)", c)
            tag = m.group(1) if m else "?"
            for line in c.splitlines():
                line = line.strip()
                if line.startswith("<defName>"):
                    dn = line[len("<defName>"):-len("</defName>")]
                    if (tag, dn) in all_def_names:
                        raise ValueError("duplicate %s %s" % (tag, dn))
                    all_def_names.add((tag, dn))
    if dry or emit:
        print(("Emitted to %s: " % out_defs if emit else "Checked (nothing written): ") +
              "%(classes)d classes, %(subclasses)d subclasses, %(spells)d spells, %(nodes)d nodes" % stats)
        return
    check_unique_def_names()
    write_keyed(keyed)
    write_json(os.path.join(HERE, "out", "icons.json"), icons)
    write_compendium(compendium)
    print("Generated: %(classes)d classes, %(subclasses)d subclasses, %(spells)d spells, %(nodes)d nodes" % stats)


def write_keyed(keys):
    """Translation strings that class modules use for status pop-ups (Seventh Seal, Enthralled...)."""
    from xml.sax.saxutils import escape
    path = os.path.join(ROOT, "Languages", "English", "Keyed", "ArcanumAscendant_Classes.xml")
    with open(path, "w", encoding="utf-8") as fh:
        fh.write('<?xml version="1.0" encoding="utf-8"?>\n<!-- Generated by Tools/gen/build.py from the class modules. -->\n<LanguageData>\n')
        for k in sorted(keys):
            fh.write("  <%s>%s</%s>\n" % (k, escape(keys[k]), k))
        fh.write("</LanguageData>\n")


def check_unique_def_names():
    """RimWorld rejects two defs of the same type with the same defName, so check every XML file, hand-written ones included."""
    import xml.etree.ElementTree as ET
    seen = {}
    for folder, _, files in os.walk(os.path.join(ROOT, "Defs")):
        for f in sorted(files):
            if not f.endswith(".xml"):
                continue
            path = os.path.join(folder, f)
            for el in ET.parse(path).getroot():
                dn = el.findtext("defName")
                if dn is None:
                    continue
                key = (el.tag, dn)
                if key in seen:
                    raise ValueError("duplicate %s %s in %s and %s" % (el.tag, dn, seen[key], os.path.relpath(path, ROOT)))
                seen[key] = os.path.relpath(path, ROOT)


SMALL_WORDS = {"of", "the", "a", "an", "and", "in", "on", "to", "for"}


def title(label):
    words = label.split(" ")
    return " ".join(w if (i and w in SMALL_WORDS) else w[:1].upper() + w[1:] for i, w in enumerate(words))


def write_compendium(all_trees):
    ATTR_NAMES = {"POT": "Potency", "FOC": "Focus", "CEL": "Celerity", "RES": "Resilience", "ATT": "Attunement"}
    lines = ["# Arcanum Ascendant - Compendium", "",
             "Every class, subclass, spell and keystone in the mod. Generated from `Tools/gen/classes/*.py` - do not edit by hand.", ""]
    total_spells = sum(len(t.spells) for trees in all_trees for t in trees)
    total_nodes = sum(len(t.nodes) for trees in all_trees for t in trees)
    lines += ["**%d classes - %d subclasses - %d spells - %d skill tree nodes**" % (len(all_trees), sum(len(t) - 1 for t in all_trees), total_spells, total_nodes), ""]
    lines += ["## Contents", ""]
    for trees in all_trees:
        r = trees[0]
        lines.append("- [%s](#%s): %s" % (title(r.label), r.label.replace(" ", "-"), ", ".join(title(t.label) for t in trees[1:])))
    lines.append("")

    def spell_table(t):
        out = ["| Spell | Mana | Cooldown | Role | Description |", "|---|---|---|---|---|"]
        for sp in t.spells:
            ticks = sp.cd * 60
            if ticks < 2500:
                cds = "%gs" % round(sp.cd, 1)
            elif ticks < 60000:
                cds = "%g h" % round(ticks / 2500, 1)
            else:
                cds = "%g day%s" % (round(ticks / 60000, 1), "" if round(ticks / 60000, 1) == 1 else "s")
            name = title(sp.label) + (" (ultimate)" if sp.ultimate else "") + (" *" if sp.quirky else "")
            out.append("| %s | %g | %s | %s | %s |" % (name, sp.mana, cds, sp.role.lower(), sp.desc.replace("|", "/")))
        return out

    for trees in all_trees:
        r = trees[0]
        lines += ["## %s" % title(r.label), "", "*%s*" % r.tagline, "", r.desc, "",
                  "**Playstyle:** " + r.playstyle, "", "**Key attributes:** %s, %s" % (ATTR_NAMES[r.primary], ATTR_NAMES[r.secondary]), "",
                  "### %s spells" % title(r.label), ""]
        lines += spell_table(r)
        ks = [n for n in r.nodes if n.kind == "Keystone"]
        if ks:
            lines += ["", "**Keystones:** " + "; ".join("*%s* - %s" % (title(n.label), n.desc or "") for n in ks)]
        lines.append("")
        for t in trees[1:]:
            lines += ["### %s (subclass)" % title(t.label), "", "*%s*" % t.tagline, "", t.desc, "", "**Playstyle:** " + t.playstyle, ""]
            lines += spell_table(t)
            ks = [n for n in t.nodes if n.kind == "Keystone"]
            if ks:
                lines += ["", "**Keystones:** " + "; ".join("*%s* - %s" % (title(n.label), n.desc or "") for n in ks)]
            lines.append("")
    lines += ["", "Spells marked * are utility or quirky spells."]
    os.makedirs(os.path.join(ROOT, "docs"), exist_ok=True)
    with open(os.path.join(ROOT, "docs", "Compendium.md"), "w", encoding="utf-8") as fh:
        fh.write("\n".join(lines) + "\n")


if __name__ == "__main__":
    main()
