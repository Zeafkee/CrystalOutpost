import math

from lowpoly import Model

PURPLE = (104, 76, 142)
PURPLE_DARK = (66, 46, 96)
RED = (255, 72, 62)
BONE = (158, 158, 182)
GREEN = (66, 128, 102)
GREEN_DARK = (40, 82, 70)
VOID = (18, 20, 28)
MAGENTA = (255, 84, 224)
WOOD = (124, 86, 60)
WOOD_DARK = (88, 60, 46)
GOLD = (244, 194, 72)
STONE = (88, 92, 124)
STONE_DARK = (56, 58, 86)
STONE_LIGHT = (128, 134, 164)
STEEL = (140, 148, 174)
STEEL_DARK = (72, 78, 106)
CYAN = (90, 235, 255)
CYAN_PALE = (190, 250, 255)
BARK = (70, 56, 84)
BANNER = (58, 44, 110)

FORWARD = (-math.pi / 2, 0, 0)


def husk(name="EnemyMelee", boss=False):
    m = Model(name)
    lean = -0.38 if not boss else -0.22

    for side, x in ((-1.0, -0.2), (1.0, 0.2)):
        m.limb("leg", side, (x, -0.05, 0.42))
        m.paint(PURPLE_DARK).box(f"Leg{side:+.0f}", (0.24, 0.3, 0.42), (x, -0.05, 0.21))

    m.limb(None)
    m.paint(PURPLE).box("Torso", (0.82, 0.7, 0.56), (0, 0.06, 0.72), rot=(lean, 0, 0), taper=(0.86, 0.82))
    m.paint(PURPLE_DARK).box("Hump", (0.62, 0.5, 0.3), (0, -0.1, 1.02), rot=(lean, 0, 0), taper=(0.8, 0.8))
    m.paint(PURPLE).box("Head", (0.4, 0.36, 0.32), (0, 0.5, 0.8), taper=(0.8, 0.8))
    m.paint(RED, 1.0).box("EyeL", (0.09, 0.03, 0.06), (-0.1, 0.685, 0.84))
    m.box("EyeR", (0.09, 0.03, 0.06), (0.1, 0.685, 0.84))

    m.paint(BONE)
    for i in range(3):
        m.prism(f"Spike{i}", 0.085, 0.3, (0, 0.0 - i * 0.2, 1.1 - i * 0.07), rot=(0.45, 0, 0), sides=4, top_radius=0)

    for side, x in ((-1.0, -0.47), (1.0, 0.47)):
        m.limb("arm", side, (x, 0.15, 0.92))
        m.paint(PURPLE).strut(f"Arm{side:+.0f}", (x, 0.15, 0.94), (x * 1.3, 0.42, 0.44), (0.2, 0.22))
        m.paint(PURPLE_DARK).box(f"Hand{side:+.0f}", (0.24, 0.26, 0.16), (x * 1.3, 0.48, 0.32))
        m.paint(RED, 0.9)
        for c in (-1, 0, 1):
            m.prism(f"Claw{side:+.0f}{c}", 0.05, 0.32, (x * 1.3 + c * 0.08, 0.58, 0.32), rot=(-1.95, 0, 0),
                    sides=4, top_radius=0)

    if boss:
        m.limb(None)
        m.paint(STONE_LIGHT).box("PlateBack", (0.7, 0.4, 0.2), (0, -0.16, 1.18), rot=(lean, 0, 0), bevel=0.03)
        m.paint(RED, 1.0).box("ChestCore", (0.3, 0.06, 0.26), (0, 0.4, 0.62), rot=(lean, 0, 0))
        m.paint(STONE).box("ChestFrame", (0.46, 0.05, 0.4), (0, 0.375, 0.62), rot=(lean, 0, 0))
        m.paint(GOLD, 0.25)
        for sx in (-1, 1):
            m.strut(f"HornA{sx}", (sx * 0.14, 0.5, 0.94), (sx * 0.42, 0.48, 1.12), (0.11, 0.11))
            m.strut(f"HornB{sx}", (sx * 0.42, 0.48, 1.12), (sx * 0.5, 0.56, 1.42), (0.09, 0.09), taper=(0.2, 0.2))
        for side, x in ((-1.0, -0.47), (1.0, 0.47)):
            m.limb("arm", side, (x, 0.15, 0.92))
            m.paint(STONE_LIGHT).box(f"Pauldron{side:+.0f}", (0.36, 0.4, 0.24), (x * 1.08, 0.12, 1.0), bevel=0.04)
            m.paint(GOLD, 0.25).prism(f"PauldronSpike{side:+.0f}", 0.08, 0.26, (x * 1.12, 0.12, 1.1),
                                      rot=(0, side * 0.5, 0), sides=4, top_radius=0)
            m.paint(STONE).box(f"Bracer{side:+.0f}", (0.26, 0.28, 0.2), (x * 1.22, 0.34, 0.6), bevel=0.03)
    return m


def spitter():
    m = Model("EnemyRanged")
    m.limb(None)
    m.paint(GREEN).prism("Robe", 0.36, 0.95, (0, 0, 0), sides=6, top_radius=0.2)
    m.paint(GREEN_DARK).prism("RobeHem", 0.4, 0.1, (0, 0, 0), sides=6, top_radius=0.37)
    m.paint(WOOD_DARK).prism("Belt", 0.25, 0.07, (0, 0, 0.56), sides=6, top_radius=0.24)
    m.paint(GREEN).box("Shoulders", (0.52, 0.32, 0.16), (0, 0, 0.99), taper=(0.78, 0.9))
    m.paint(GREEN_DARK).prism("Hood", 0.27, 0.42, (0, -0.01, 1.05), sides=6, top_radius=0.09)
    m.paint(VOID).box("HoodDark", (0.26, 0.06, 0.2), (0, 0.19, 1.2))
    m.paint(MAGENTA, 1.0).box("Eye", (0.2, 0.03, 0.05), (0, 0.225, 1.22))

    m.limb("arm", -1.0, (-0.28, 0, 1.0))
    m.paint(GREEN).strut("ArmL", (-0.27, 0, 1.0), (-0.3, 0.2, 0.72), (0.12, 0.12))

    m.limb("arm", 1.0, (0.28, 0, 1.0))
    m.paint(GREEN).strut("ArmR", (0.27, 0, 1.0), (0.37, 0.26, 0.8), (0.12, 0.12))
    m.paint(WOOD).strut("Staff", (0.38, 0.3, 0.02), (0.38, 0.3, 1.52), (0.055, 0.055))
    m.paint(WOOD_DARK).box("StaffHead", (0.16, 0.1, 0.08), (0.38, 0.3, 1.54))
    m.paint(MAGENTA, 1.0).gem("StaffCrystal", 0.12, 0.38, (0.38, 0.3, 1.56), sides=4, waist=0.45)
    return m


def core():
    m = Model("Core")
    m.limb(None)
    eighth = math.pi / 8
    m.paint(STONE_DARK).prism("Step1", 2.0, 0.3, (0, 0, 0), sides=8, top_radius=1.85, spin=eighth)
    m.paint(STONE).prism("Step2", 1.55, 0.3, (0, 0, 0.3), sides=8, top_radius=1.4, spin=eighth)
    m.paint(CYAN, 0.9).prism("Ring", 1.02, 0.07, (0, 0, 0.6), sides=8, spin=eighth)
    m.paint(STONE_DARK).prism("RingInner", 0.8, 0.09, (0, 0, 0.6), sides=8, spin=eighth)
    m.paint(CYAN, 1.0).prism("RingCore", 0.4, 0.1, (0, 0, 0.61), sides=8, spin=eighth)

    for i in range(4):
        a = math.pi / 4 + i * math.pi / 2
        x, y = math.cos(a) * 1.62, math.sin(a) * 1.62
        m.paint(STONE).box(f"Pylon{i}", (0.5, 0.5, 0.75), (x, y, 0.5), rot=(0, 0, a), taper=(0.8, 0.8), bevel=0.04)
        m.paint(CYAN, 1.0).gem(f"PylonCrystal{i}", 0.14, 0.5, (x, y, 0.9), sides=4, waist=0.4)

    for i in range(4):
        a = i * math.pi / 2
        x, y = math.cos(a) * 1.45, math.sin(a) * 1.45
        m.paint(CYAN, 1.0).box(f"Slit{i}", (0.07, 0.34, 0.2), (x, y, 0.45), rot=(0, 0, a))
    return m


def core_crystal():
    m = Model("CoreCrystal")
    m.limb(None)
    m.paint(CYAN, 1.0).gem("Main", 0.62, 2.3, (0, 0, -1.05), sides=6, waist=0.36)
    m.paint(CYAN_PALE, 1.0)
    for i in range(4):
        a = 0.6 + i * math.pi / 2
        m.gem(f"Shard{i}", 0.11, 0.42, (math.cos(a) * 1.0, math.sin(a) * 1.0, -0.3 + (i % 2) * 0.5), sides=4, waist=0.45)
    return m


def turret_base():
    m = Model("TurretBase")
    m.limb(None)
    m.paint(STONE_DARK).box("Foot", (1.4, 1.4, 0.45), (0, 0, 0.225), taper=(0.84, 0.84), bevel=0.04)
    m.paint(STONE).box("Tower", (0.82, 0.82, 1.05), (0, 0, 0.97), taper=(0.72, 0.72), bevel=0.035)
    m.paint(WOOD)
    for sx in (-1, 1):
        for sy in (-1, 1):
            m.strut(f"Leg{sx}{sy}", (sx * 0.56, sy * 0.56, 0.42), (sx * 0.34, sy * 0.34, 1.52), (0.14, 0.14))
    m.paint(WOOD_DARK).box("Platform", (0.96, 0.96, 0.12), (0, 0, 1.56), bevel=0.02)
    m.paint(BANNER).box("Banner", (0.3, 0.04, 0.52), (0, 0.4, 0.98), rot=(0.1, 0, 0))
    m.paint(CYAN, 1.0).gem("BannerMark", 0.07, 0.2, (0, 0.43, 0.9), rot=(0.1, 0, 0), sides=4, waist=0.5)
    return m


def turret_head():
    m = Model("TurretHead")
    m.limb(None)
    m.paint(STEEL_DARK).box("Mount", (0.34, 0.34, 0.18), (0, 0, 0.09), bevel=0.03)
    m.paint(WOOD).box("Stock", (0.2, 1.0, 0.16), (0, 0.16, 0.27), bevel=0.025)
    m.paint(WOOD_DARK)
    for sx in (-1, 1):
        m.strut(f"Bow{sx}", (0, 0.56, 0.27), (sx * 0.72, 0.26, 0.27), (0.09, 0.11), taper=(0.5, 0.6))
    m.paint(STEEL).box("BowCap", (0.26, 0.14, 0.2), (0, 0.58, 0.27), bevel=0.02)
    m.paint(CYAN, 1.0).gem("Power", 0.12, 0.32, (0, -0.22, 0.34), sides=4, waist=0.45)
    m.paint(GOLD, 0.5).strut("Bolt", (0, 0.05, 0.38), (0, 0.9, 0.38), (0.05, 0.05))
    m.paint(CYAN, 1.0).prism("BoltTip", 0.07, 0.2, (0, 0.9, 0.38), rot=FORWARD, sides=4, top_radius=0)
    return m


def cannon_base():
    m = Model("CannonBase")
    m.limb(None)
    eighth = math.pi / 8
    m.paint(STONE_DARK).prism("Foot", 1.05, 0.5, (0, 0, 0), sides=8, top_radius=0.86, spin=eighth)
    m.paint(STONE).prism("Dome", 0.8, 0.42, (0, 0, 0.5), sides=8, top_radius=0.56, spin=eighth)
    m.paint(STEEL_DARK).prism("Collar", 0.5, 0.1, (0, 0, 0.92), sides=8, spin=eighth)
    for i in range(4):
        a = i * math.pi / 2
        m.paint(CYAN, 1.0).box(f"Slit{i}", (0.06, 0.2, 0.26), (math.cos(a) * 0.93, math.sin(a) * 0.93, 0.26), rot=(0, 0, a))
    return m


def cannon_head():
    m = Model("CannonHead")
    m.limb(None)
    m.paint(STEEL_DARK).box("Mount", (0.5, 0.5, 0.2), (0, 0, 0.1), bevel=0.04)
    m.paint(STONE_DARK).prism("Barrel", 0.3, 1.05, (0, -0.3, 0.42), rot=FORWARD, sides=8)
    m.paint(CYAN, 1.0).prism("RingBack", 0.34, 0.1, (0, 0.05, 0.42), rot=FORWARD, sides=8)
    m.prism("RingFront", 0.36, 0.12, (0, 0.6, 0.42), rot=FORWARD, sides=8)
    m.paint(CYAN_PALE, 1.0).prism("Bore", 0.2, 0.04, (0, 0.74, 0.42), rot=FORWARD, sides=8)
    for sx in (-1, 1):
        m.paint(STONE).box(f"Pod{sx}", (0.2, 0.44, 0.3), (sx * 0.4, -0.1, 0.42), bevel=0.03)
        m.paint(CYAN, 1.0).box(f"PodLight{sx}", (0.06, 0.24, 0.12), (sx * 0.5, -0.1, 0.42))
    return m


def barricade():
    m = Model("Barricade")
    m.limb(None)
    heights = (0.95, 1.12, 0.88, 1.08, 0.98)
    for i, h in enumerate(heights):
        x = -0.8 + i * 0.4
        m.paint(WOOD if i % 2 == 0 else WOOD_DARK).box(f"Stake{i}", (0.34, 0.24, h), (x, 0, h * 0.5))
        m.prism(f"Point{i}", 0.22, 0.3, (x, 0, h), sides=4, top_radius=0, spin=math.pi / 4)
    m.paint(STEEL_DARK).box("BandLow", (2.04, 0.3, 0.12), (0, 0, 0.32), bevel=0.02)
    m.box("BandHigh", (2.04, 0.3, 0.12), (0, 0, 0.72), bevel=0.02)
    m.paint(STEEL)
    for x in (-0.8, 0.0, 0.8):
        m.box(f"Bolt{x}", (0.08, 0.34, 0.08), (x, 0, 0.32))
        m.box(f"BoltH{x}", (0.08, 0.34, 0.08), (x, 0, 0.72))
    return m


def build_pad():
    m = Model("BuildPad")
    m.limb(None)
    m.paint((34, 36, 58)).box("Plate", (2.5, 2.5, 0.04), (0, 0, 0.02))
    half = 1.3
    for sx in (-1, 1):
        for sy in (-1, 1):
            m.paint(STONE_DARK).box(f"Corner{sx}{sy}", (0.4, 0.4, 0.16), (sx * half, sy * half, 0.08), bevel=0.03)
            m.paint(CYAN, 1.0).box(f"CornerLight{sx}{sy}", (0.18, 0.18, 0.04), (sx * half, sy * half, 0.17))
    m.paint(CYAN, 1.0)
    for d in (-0.6, 0.0, 0.6):
        for s in (-1, 1):
            m.box(f"DashX{d}{s}", (0.36, 0.1, 0.05), (d, s * half, 0.045))
            m.box(f"DashY{d}{s}", (0.1, 0.36, 0.05), (s * half, d, 0.045))
    return m


def rock():
    m = Model("Rock")
    m.limb(None)
    m.paint(STONE).prism("A", 0.85, 0.8, (0, 0, 0), rot=(0.08, -0.06, 0), sides=6, top_radius=0.5, spin=0.3)
    m.paint(STONE_DARK).prism("B", 0.55, 0.5, (0.62, 0.3, 0), rot=(-0.1, 0.12, 0), sides=5, top_radius=0.3, spin=1.1)
    m.paint(STONE_LIGHT).prism("C", 0.38, 0.34, (-0.55, -0.4, 0), rot=(0.1, 0.1, 0), sides=5, top_radius=0.2, spin=0.6)
    m.paint(STONE_DARK).prism("D", 0.22, 0.2, (0.2, -0.7, 0), sides=4, top_radius=0.12, spin=0.4)
    return m


def crystal_vein():
    m = Model("CrystalVein")
    m.limb(None)
    m.paint(STONE_DARK).prism("RockA", 0.95, 0.4, (0, 0, 0), sides=6, top_radius=0.7, spin=0.2)
    m.paint(STONE).prism("RockB", 0.5, 0.5, (0.55, -0.3, 0), rot=(0, 0.2, 0), sides=5, top_radius=0.28, spin=0.9)
    m.paint(STONE_DARK).prism("RockC", 0.45, 0.42, (-0.5, 0.35, 0), rot=(0.15, -0.1, 0), sides=5, top_radius=0.25, spin=0.5)
    m.paint(CYAN, 0.9).gem("Main", 0.3, 1.7, (0, 0, 0.25), sides=5, waist=0.3)
    m.gem("Left", 0.21, 1.1, (-0.36, -0.08, 0.3), rot=(0, -0.42, 0), sides=5, waist=0.3)
    m.gem("Right", 0.2, 1.0, (0.36, 0.12, 0.3), rot=(0.1, 0.45, 0), sides=5, waist=0.3)
    m.paint(CYAN_PALE, 1.0).gem("Front", 0.15, 0.7, (0.05, 0.42, 0.3), rot=(-0.5, 0, 0), sides=4, waist=0.35)
    m.gem("Small", 0.1, 0.4, (-0.6, -0.5, 0.05), rot=(0.3, -0.3, 0), sides=4, waist=0.35)
    return m


def dead_tree():
    m = Model("DeadTree")
    m.limb(None)
    m.paint(BARK)
    m.strut("Trunk1", (0, 0, 0), (0.1, 0.02, 1.0), (0.36, 0.36), taper=(0.72, 0.72))
    m.strut("Trunk2", (0.1, 0.02, 1.0), (-0.06, 0.06, 1.9), (0.26, 0.26), taper=(0.7, 0.7))
    m.strut("Trunk3", (-0.06, 0.06, 1.9), (0.04, 0.0, 2.6), (0.18, 0.18), taper=(0.3, 0.3))
    branches = [
        ((0.08, 0.02, 1.1), (0.75, 0.1, 1.7), 0.13), ((0.75, 0.1, 1.7), (0.95, 0.05, 2.25), 0.08),
        ((0.0, 0.04, 1.5), (-0.7, -0.15, 2.0), 0.12), ((-0.7, -0.15, 2.0), (-1.05, -0.1, 2.3), 0.07),
        ((-0.04, 0.05, 1.8), (0.1, 0.6, 2.35), 0.1), ((0.02, 0.02, 2.2), (-0.3, -0.45, 2.7), 0.08),
    ]
    for i, (a, b, w) in enumerate(branches):
        m.strut(f"Branch{i}", a, b, (w, w), taper=(0.45, 0.45))
    for i, (x, y, rz) in enumerate(((0.3, 0.1, 0.2), (-0.25, 0.2, 2.2), (0.0, -0.3, 4.2))):
        m.prism(f"Root{i}", 0.16, 0.3, (x, y, 0), rot=(0.0, 0.0, rz), sides=4, top_radius=0.05)
    return m


def crystal():
    m = Model("Crystal")
    m.limb(None)
    m.paint(CYAN, 0.9).gem("Gem", 0.17, 0.52, (0, 0, -0.23), sides=5, waist=0.42)
    return m


def bolt():
    m = Model("Bolt")
    m.limb(None)
    m.paint(GOLD, 0.7).strut("Shaft", (0, -0.45, 0), (0, 0.36, 0), (0.06, 0.06))
    m.paint((235, 240, 255), 0.8).prism("Head", 0.1, 0.24, (0, 0.36, 0), rot=FORWARD, sides=4, top_radius=0)
    m.paint(WOOD).box("FletchH", (0.2, 0.16, 0.02), (0, -0.4, 0))
    m.box("FletchV", (0.02, 0.16, 0.2), (0, -0.4, 0))
    return m


def shell():
    m = Model("Shell")
    m.limb(None)
    m.paint(CYAN, 1.0).gem("Shard", 0.15, 0.78, (0, -0.34, 0), rot=FORWARD, sides=5, waist=0.35)
    return m


def enemy_orb():
    m = Model("EnemyOrb")
    m.limb(None)
    m.paint(MAGENTA, 1.0).gem("Orb", 0.2, 0.4, (0, 0, -0.2), sides=6, waist=0.5)
    m.paint((255, 170, 240), 1.0)
    for i in range(4):
        a = i * math.pi / 2
        m.prism(f"Spark{i}", 0.05, 0.16, (math.cos(a) * 0.22, math.sin(a) * 0.22, 0), rot=(0, math.pi / 2, a),
                sides=3, top_radius=0)
    return m


ALL = [
    lambda: husk("EnemyMelee"), spitter, lambda: husk("EnemyBoss", boss=True),
    core, core_crystal, turret_base, turret_head, cannon_base, cannon_head, barricade, build_pad,
    rock, crystal_vein, dead_tree, crystal, bolt, shell, enemy_orb,
]
