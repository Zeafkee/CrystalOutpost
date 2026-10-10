import math

from lowpoly import Model

BLUE = (56, 90, 178)
BLUE_DARK = (36, 56, 124)
STEEL = (140, 148, 174)
STEEL_DARK = (78, 84, 112)
SUIT = (40, 42, 62)
GUN = (60, 64, 88)
BROWN = (112, 80, 58)
CYAN = (90, 235, 255)


def build():
    m = Model("Hero")

    for side, x in ((-1.0, -0.17), (1.0, 0.17)):
        m.limb("leg", side, (x, 0.0, 0.58))
        m.paint(SUIT).box(f"Thigh{side:+.0f}", (0.24, 0.28, 0.34), (x, 0.0, 0.42), bevel=0.02)
        m.paint(STEEL).box(f"Knee{side:+.0f}", (0.2, 0.08, 0.15), (x, 0.15, 0.37), bevel=0.015)
        m.paint(STEEL_DARK).box(f"Boot{side:+.0f}", (0.28, 0.42, 0.22), (x, 0.05, 0.11), taper=(0.86, 0.8), bevel=0.03)
        m.paint(STEEL).box(f"Toe{side:+.0f}", (0.27, 0.15, 0.12), (x, 0.21, 0.07), bevel=0.02)

    m.limb(None)
    m.paint(SUIT).box("Pelvis", (0.56, 0.36, 0.18), (0, 0, 0.64), bevel=0.02)
    m.paint(BROWN).box("Belt", (0.6, 0.4, 0.07), (0, 0, 0.72))
    m.box("Pouch", (0.15, 0.1, 0.15), (0.25, 0.17, 0.65), bevel=0.015)
    m.paint(BLUE).box("Torso", (0.6, 0.4, 0.42), (0, 0, 0.95), taper=(1.18, 1.1), bevel=0.03)
    m.paint(STEEL).box("ChestPlate", (0.48, 0.1, 0.3), (0, 0.2, 0.98), taper=(1.12, 1.0), bevel=0.025)
    m.paint(CYAN, 1.0).box("ChestLight", (0.12, 0.04, 0.1), (0, 0.262, 1.0))
    m.paint(BLUE_DARK).box("Collar", (0.68, 0.52, 0.14), (0, -0.02, 1.2), bevel=0.035)

    for x, tilt in ((-0.46, 0.3), (0.46, -0.3)):
        m.paint(STEEL).box(f"Pad{x:+.1f}", (0.3, 0.38, 0.2), (x, 0.0, 1.13), rot=(0, tilt, 0), bevel=0.045)
        m.paint(STEEL_DARK).box(f"PadTrim{x:+.1f}", (0.24, 0.32, 0.08), (x * 1.04, 0.0, 1.0), rot=(0, tilt, 0))

    m.paint(BLUE_DARK).box("Cape", (0.9, 0.06, 0.98), (0, -0.34, 0.7), taper=(0.62, 1.0), top_shift=(0, 0.12))
    m.paint(BLUE).box("CapeTop", (0.5, 0.08, 0.2), (0, -0.24, 1.1), bevel=0.02)

    m.paint(BLUE).box("Helmet", (0.64, 0.62, 0.52), (0, 0.02, 1.5), taper=(0.8, 0.84), bevel=0.07)
    m.paint(SUIT).box("Face", (0.42, 0.06, 0.26), (0, 0.315, 1.45), bevel=0.02)
    m.paint(CYAN, 1.0).box("VisorL", (0.21, 0.035, 0.075), (-0.095, 0.35, 1.47), rot=(0, 0.45, 0))
    m.box("VisorR", (0.21, 0.035, 0.075), (0.095, 0.35, 1.47), rot=(0, -0.45, 0))
    m.paint(STEEL).box("Crest", (0.2, 0.6, 0.1), (0, -0.02, 1.8), taper=(0.75, 0.85), bevel=0.03)
    m.box("Brow", (0.2, 0.1, 0.3), (0, 0.31, 1.68), rot=(-0.28, 0, 0), bevel=0.02)
    for x in (-0.31, 0.31):
        m.paint(STEEL_DARK).box(f"Cheek{x:+.1f}", (0.08, 0.3, 0.26), (x, 0.13, 1.42), bevel=0.02)

    m.limb("held", 1.0, (0.0, 0.2, 1.0))
    m.paint(BLUE).strut("ArmR", (0.44, 0.0, 1.06), (0.34, 0.3, 0.86), (0.16, 0.16))
    m.paint(STEEL_DARK).strut("GloveR", (0.34, 0.3, 0.86), (0.17, 0.48, 0.84), (0.15, 0.15))
    m.paint(BLUE).strut("ArmL", (-0.44, 0.0, 1.06), (-0.3, 0.42, 0.9), (0.16, 0.16))
    m.paint(STEEL_DARK).strut("GloveL", (-0.3, 0.42, 0.9), (0.0, 0.72, 0.8), (0.15, 0.15))

    forward = (-math.pi / 2, 0, 0)
    m.paint(GUN).box("RifleBody", (0.17, 0.78, 0.22), (0.1, 0.66, 0.84), bevel=0.03)
    m.box("RifleStock", (0.12, 0.26, 0.16), (0.14, 0.2, 0.86), bevel=0.02)
    m.paint(STEEL_DARK).box("RifleRail", (0.08, 0.5, 0.06), (0.1, 0.62, 0.98))
    m.paint(CYAN, 1.0).box("RifleGlow", (0.19, 0.4, 0.07), (0.1, 0.74, 0.84))
    m.paint(GUN).prism("RifleBarrel", 0.075, 0.34, (0.1, 1.04, 0.84), rot=forward, sides=6)
    m.paint(CYAN, 1.0).prism("RifleMuzzle", 0.095, 0.08, (0.1, 1.36, 0.84), rot=forward, sides=6)
    return m
