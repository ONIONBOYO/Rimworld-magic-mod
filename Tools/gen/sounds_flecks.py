"""SoundDefs and FleckDefs."""
from arcgen import O, to_xml

SCHOOLS = ["Fire", "Holy", "Rune", "Frost", "Storm", "Necro", "Shadow", "Arcane", "Blood", "Nature", "Song", "Void", "Time"]

# name -> (volume range, pitch range, on camera)
SOUNDS = {"AA_Charge": ("18~24", "0.9~1.1", False), "AA_Turret_Shot": ("14~20", "0.85~1.15", False), "AA_Construct_Break": ("25~30", "0.9~1.1", False),
          "AA_LevelUp": ("35~40", "1~1", False), "AA_Awaken": ("40~45", "1~1", False), "AA_NodeUnlock": ("30~35", "0.95~1.05", True)}
for s in SCHOOLS:
    SOUNDS["AA_Cast_" + s] = ("22~30", "0.9~1.1", False)
    SOUNDS["AA_Impact_" + s] = ("30~38", "0.88~1.12", False)


def sound_xml():
    out = []
    for name, (vol, pitch, cam) in SOUNDS.items():
        folder = "ArcanumAscendant/" + name[3:]
        sub = O(None, grains=[O("AudioGrain_Folder", clipFolderPath=folder)], volumeRange=vol, pitchRange=pitch,
                distRange=None if cam else "25~70", onCamera=True if cam else None)
        out.append(to_xml("SoundDef", O(None, defName=name, context="Any" if cam else "MapOnly", maxSimultaneous=4, subSounds=[sub]), 1))
    return out


# name -> (tex, fade in, solid, fade out, growth, shader, rotate toward move)
FLECKS = {
    "Spark": ("Spark", 0.02, 0.15, 0.35, -0.6, "MoteGlow"),
    "Glow": ("Glow", 0.05, 0.1, 0.5, 0.4, "MoteGlow"),
    "Ember": ("Ember", 0.05, 0.3, 0.6, -0.3, "MoteGlow"),
    "Mote": ("Mote", 0.05, 0.2, 0.5, -0.4, "MoteGlow"),
    "Smoke": ("Smoke", 0.2, 0.4, 1.4, 0.6, "Mote"),
    "Shard": ("Shard", 0.02, 0.3, 0.4, -0.2, "MoteGlow"),
    "Leaf": ("Leaf", 0.1, 0.6, 0.8, 0.0, "Mote"),
    "Note": ("Note", 0.1, 0.5, 0.6, 0.0, "MoteGlow"),
    "Rune": ("Rune", 0.15, 0.4, 0.6, 0.1, "MoteGlow"),
    "Ring": ("Ring", 0.02, 0.1, 0.4, 3.0, "MoteGlow"),
    "Snow": ("Snow", 0.1, 0.6, 0.6, 0.0, "MoteGlow"),
    "Bone": ("Bone", 0.05, 0.4, 0.5, 0.0, "Mote"),
    "Feather": ("Feather", 0.1, 0.5, 0.7, 0.0, "MoteGlow"),
    "Blood": ("BloodDrop", 0.02, 0.3, 0.4, -0.2, "Mote"),
    "Gear": ("Gear", 0.05, 0.4, 0.5, 0.0, "MoteGlow"),
    "Star": ("Star", 0.05, 0.3, 0.6, -0.3, "MoteGlow"),
    "Skull": ("Skull", 0.1, 0.4, 0.6, 0.2, "MoteGlow"),
    "Petal": ("Petal", 0.1, 0.6, 0.8, 0.0, "Mote"),
    "Clock": ("Clock", 0.1, 0.4, 0.6, 0.1, "MoteGlow"),
    "Bolt": ("Bolt", 0.01, 0.1, 0.2, 0.0, "MoteGlow"),
}


def fleck_xml():
    out = []
    for name, (tex, fi, so, fo, gr, sh) in FLECKS.items():
        out.append(to_xml("FleckDef", O(None, defName="AA_Fleck_" + name, fleckSystemClass="FleckSystemThrown",
            graphicData=O(None, texPath="ArcanumAscendant/Vfx/" + tex, graphicClass="Graphic_Fleck", shaderType=sh),
            altitudeLayer="MoteOverhead", fadeInTime=fi, solidTime=so, fadeOutTime=fo, growthRate=gr if gr else None), 1))
    return out
