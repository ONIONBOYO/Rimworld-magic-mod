// Validates the mod's XML defs against the compiled assembly and RimWorld reference assemblies.
// Usage: dotnet run -- <modRoot> <rimworldRefDir>
using System.Reflection;
using System.Xml;

class V
{
    static MetadataLoadContext mlc;
    static Assembly game, mod;
    static List<Type> allTypes = new();
    static Dictionary<string, Type> byName = new();
    static Dictionary<string, HashSet<string>> defs = new();   // defType short name -> defNames
    static HashSet<string> knownVanilla = new();
    static int errors, warnings;
    static HashSet<string> unknownRefs = new();

    static void Main(string[] a)
    {
        string root = a[0], refDir = a[1];
        var paths = Directory.GetFiles(refDir, "*.dll").ToList();
        paths.Add(Path.Combine(root, "1.6/Assemblies/ArcanumAscendant.dll"));
        mlc = new MetadataLoadContext(new PathAssemblyResolver(paths), "mscorlib");
        game = mlc.LoadFromAssemblyPath(Path.Combine(refDir, "Assembly-CSharp.dll"));
        mod = mlc.LoadFromAssemblyPath(Path.Combine(root, "1.6/Assemblies/ArcanumAscendant.dll"));
        foreach (var asm in new[] { game, mod, mlc.LoadFromAssemblyPath(Path.Combine(refDir, "UnityEngine.CoreModule.dll")) })
        {
            Type[] ts; try { ts = asm.GetTypes(); } catch (ReflectionTypeLoadException e) { ts = e.Types.Where(t => t != null).ToArray(); }
            foreach (var t in ts) { allTypes.Add(t); if (t.FullName != null) byName[t.FullName] = t; }
        }
        // vanilla def names from DefOf classes
        foreach (var t in allTypes.Where(t => t.Name.EndsWith("DefOf")))
            foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.Static)) knownVanilla.Add(f.Name);
        var extraKnown = Path.Combine(root, "Tools/validator/known_vanilla_defs.txt");
        if (File.Exists(extraKnown)) foreach (var l in File.ReadAllLines(extraKnown)) if (l.Trim().Length > 0 && !l.StartsWith("#")) knownVanilla.Add(l.Trim());

        var files = Directory.GetFiles(Path.Combine(root, "Defs"), "*.xml", SearchOption.AllDirectories);
        var docs = files.Select(f => (f, doc: Load(f))).ToList();
        // collect defNames
        foreach (var (f, doc) in docs)
            foreach (XmlNode n in doc.DocumentElement.ChildNodes)
            {
                if (n is not XmlElement e) continue;
                var dn = e["defName"]?.InnerText; var nameAttr = e.GetAttribute("Name");
                var t = ResolveDefType(e.Name);
                if (t == null) { Err(f, $"unknown def type {e.Name}"); continue; }
                for (var bt = t; bt != null && bt.FullName != "Verse.Def"; bt = bt.BaseType)
                {
                    if (!defs.TryGetValue(bt.Name, out var set)) defs[bt.Name] = set = new();
                    if (dn != null) set.Add(dn);
                }
            }
        foreach (var (f, doc) in docs)
            foreach (XmlNode n in doc.DocumentElement.ChildNodes)
            {
                if (n is not XmlElement e) continue;
                var t = ResolveDefType(e.Name);
                if (t == null) continue;
                CheckObject(f, e, t, e["defName"]?.InnerText ?? e.GetAttribute("Name"));
            }
        Console.WriteLine($"Checked {files.Length} files. {errors} errors, {warnings} warnings.");
        if (unknownRefs.Count > 0)
        {
            Console.WriteLine("Unverified vanilla def references (review manually or add to known_vanilla_defs.txt):");
            foreach (var u in unknownRefs.OrderBy(x => x)) Console.WriteLine("  " + u);
        }
        Environment.Exit(errors > 0 ? 1 : 0);
    }

    static XmlDocument Load(string f) { var d = new XmlDocument(); d.Load(f); return d; }
    static void Err(string f, string m) { errors++; Console.WriteLine($"ERROR {Path.GetFileName(f)}: {m}"); }
    static void Warn(string f, string m) { warnings++; Console.WriteLine($"warn  {Path.GetFileName(f)}: {m}"); }

    static Type ResolveDefType(string name)
    {
        if (name.Contains('.')) return byName.GetValueOrDefault(name);
        foreach (var ns in new[] { "Verse.", "RimWorld.", "Verse.Sound." })
            if (byName.TryGetValue(ns + name, out var t)) return t;
        return null;
    }

    static Type ResolveClass(string name)
    {
        if (byName.TryGetValue(name, out var t)) return t;
        foreach (var ns in new[] { "Verse.", "RimWorld.", "Verse.AI.", "Verse.Sound.", "RimWorld.Planet." })
            if (byName.TryGetValue(ns + name, out t)) return t;
        return null;
    }

    static bool IsDef(Type t) { for (var b = t; b != null; b = b.BaseType) if (b.FullName == "Verse.Def") return true; return false; }
    static bool HasCustomLoader(Type t) => t.GetMethod("LoadDataFromXmlCustom", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) != null;

    static FieldInfo FindField(Type t, string name)
    {
        for (var b = t; b != null; b = b.BaseType)
        {
            var f = b.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (f != null) return f;
        }
        return null;
    }

    static void CheckObject(string file, XmlElement e, Type t, string ctx)
    {
        var cls = e.GetAttribute("Class");
        if (!string.IsNullOrEmpty(cls))
        {
            var ct = ResolveClass(cls);
            if (ct == null) { Err(file, $"{ctx}: unknown Class=\"{cls}\""); return; }
            if (!IsAssignable(t, ct)) { Err(file, $"{ctx}: Class {cls} is not a {t.FullName}"); return; }
            t = ct;
        }
        if (HasCustomLoader(t)) return;
        foreach (XmlNode cn in e.ChildNodes)
        {
            if (cn is not XmlElement c) continue;
            var f = FindField(t, c.Name);
            if (f == null) { Err(file, $"{ctx}: {t.Name} has no field '{c.Name}'"); continue; }
            CheckValue(file, c, f.FieldType, $"{ctx}.{c.Name}");
        }
    }

    static bool IsAssignable(Type baseT, Type t)
    {
        for (var b = t; b != null; b = b.BaseType) if (b.FullName == baseT.FullName) return true;
        return false;
    }

    static void CheckValue(string file, XmlElement c, Type ft, string ctx)
    {
        if (ft.IsGenericType && ft.GetGenericTypeDefinition().FullName == "System.Nullable`1") ft = ft.GetGenericArguments()[0];
        if (ft.IsGenericType && ft.GetGenericTypeDefinition().FullName == "System.Collections.Generic.List`1")
        {
            var et = ft.GetGenericArguments()[0];
            if (HasCustomLoader(et) && !c.ChildNodes.OfType<XmlElement>().All(x => x.Name == "li")) return; // e.g. <statOffsets><Stat>1</Stat>
            foreach (var li in c.ChildNodes.OfType<XmlElement>())
            {
                if (li.Name != "li") { if (HasCustomLoader(et)) continue; Err(file, $"{ctx}: expected <li> got <{li.Name}>"); continue; }
                CheckValue(file, li, et, ctx + "[]");
            }
            return;
        }
        if (IsDef(ft))
        {
            var name = c.InnerText.Trim();
            if (defs.TryGetValue(ft.Name, out var set) && set.Contains(name)) return;
            // any def of subclass type
            foreach (var kv in defs) if (kv.Value.Contains(name) && SubclassName(kv.Key, ft)) return;
            if (knownVanilla.Contains(name)) return;
            if (name.StartsWith("AA_")) { Err(file, $"{ctx}: unknown {ft.Name} '{name}'"); return; }
            unknownRefs.Add($"{ft.Name}:{name}");
            return;
        }
        if (ft.IsEnum)
        {
            var names = ft.GetFields(BindingFlags.Public | BindingFlags.Static).Select(f => f.Name).ToHashSet();
            foreach (var part in c.InnerText.Split(',')) if (!names.Contains(part.Trim())) Err(file, $"{ctx}: '{c.InnerText}' is not a {ft.Name} ({string.Join("|", names)})");
            return;
        }
        if (ft.IsPrimitive || ft.FullName == "System.String") 
        {
            var v = c.InnerText.Trim();
            if (ft.FullName == "System.Single" && !float.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _)) Err(file, $"{ctx}: bad float '{v}'");
            if (ft.FullName == "System.Int32" && !int.TryParse(v, out _)) Err(file, $"{ctx}: bad int '{v}'");
            if (ft.FullName == "System.Boolean" && !bool.TryParse(v, out _)) Err(file, $"{ctx}: bad bool '{v}'");
            return;
        }
        if (ft.FullName == "System.Type")
        {
            if (ResolveClass(c.InnerText.Trim()) == null) Err(file, $"{ctx}: unknown type '{c.InnerText}'");
            return;
        }
        if (ft.IsValueType) return; // Color, IntRange, Vector3, etc.
        if (c.ChildNodes.OfType<XmlElement>().Any() || c.HasAttribute("Class")) CheckObject(file, c, ft, ctx);
    }

    static bool SubclassName(string shortName, Type baseT)
    {
        var t = allTypes.FirstOrDefault(x => x.Name == shortName);
        return t != null && IsAssignable(baseT, t);
    }
}
