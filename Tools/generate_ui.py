import math
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

OUT = Path(__file__).resolve().parents[1] / "Assets" / "_Project" / "Art" / "UI"
SS = 4


def canvas(size):
    return Image.new("RGBA", (size * SS, size * SS), (0, 0, 0, 0))


def finish(img, size):
    return img.resize((size, size), Image.LANCZOS)


def circle(size=128):
    img = canvas(size)
    s = img.size[0]
    ImageDraw.Draw(img).ellipse([2, 2, s - 3, s - 3], fill=(255, 255, 255, 255))
    return finish(img, size)


def ring(size=256, width=0.06):
    img = canvas(size)
    s = img.size[0]
    ImageDraw.Draw(img).ellipse([4, 4, s - 5, s - 5], outline=(255, 255, 255, 255), width=int(s * width))
    return finish(img, size)


def rounded(size=64, radius=20):
    img = canvas(size)
    s = img.size[0]
    ImageDraw.Draw(img).rounded_rectangle([0, 0, s - 1, s - 1], radius=radius * SS, fill=(255, 255, 255, 255))
    return finish(img, size)


def vignette(size=256):
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    px = img.load()
    c = (size - 1) / 2
    for y in range(size):
        for x in range(size):
            d = math.hypot((x - c) / c, (y - c) / c) / 1.25
            a = max(0.0, min(1.0, (d - 0.45) / 0.55)) ** 1.6
            px[x, y] = (4, 3, 14, int(235 * a))
    return img


def crystal_icon(size=128):
    img = canvas(size)
    s = img.size[0]
    d = ImageDraw.Draw(img)
    top, mid, bottom = s * 0.1, s * 0.4, s * 0.92
    left, right, cx = s * 0.14, s * 0.86, s * 0.5
    facets = [
        ([(cx, top), (right, mid), (cx, mid)], (150, 245, 255)),
        ([(cx, top), (left, mid), (cx, mid)], (210, 252, 255)),
        ([(left, mid), (cx, mid), (cx, bottom)], (70, 200, 240)),
        ([(right, mid), (cx, mid), (cx, bottom)], (40, 150, 215)),
    ]
    glow = canvas(size)
    ImageDraw.Draw(glow).ellipse([s * 0.05, s * 0.1, s * 0.95, s * 0.95], fill=(80, 230, 255, 90))
    img.alpha_composite(glow.filter(ImageFilter.GaussianBlur(s * 0.06)))
    d = ImageDraw.Draw(img)
    for pts, rgb in facets:
        d.polygon(pts, fill=rgb + (255,))
    d.line([(cx, top), (left, mid), (cx, bottom), (right, mid), (cx, top)], fill=(20, 90, 150, 255), width=int(s * 0.03), joint="curve")
    return finish(img, size)


def skull_icon(size=96):
    img = canvas(size)
    s = img.size[0]
    d = ImageDraw.Draw(img)
    w = (255, 255, 255, 255)
    d.rounded_rectangle([s * 0.24, s * 0.3, s * 0.76, s * 0.82], radius=s * 0.16, fill=w)
    d.polygon([(s * 0.24, s * 0.42), (s * 0.08, s * 0.1), (s * 0.4, s * 0.32)], fill=w)
    d.polygon([(s * 0.76, s * 0.42), (s * 0.92, s * 0.1), (s * 0.6, s * 0.32)], fill=w)
    d.ellipse([s * 0.33, s * 0.46, s * 0.45, s * 0.58], fill=(0, 0, 0, 0))
    d.ellipse([s * 0.55, s * 0.46, s * 0.67, s * 0.58], fill=(0, 0, 0, 0))
    return finish(img, size)


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    circle().save(OUT / "Circle.png")
    ring().save(OUT / "Ring.png")
    rounded().save(OUT / "Rounded_9s.png")
    rounded(64, 31).save(OUT / "Pill_9s.png")
    vignette().save(OUT / "Vignette.png")
    crystal_icon().save(OUT / "CrystalIcon.png")
    skull_icon().save(OUT / "EnemyIcon.png")
    print("done ->", OUT)


if __name__ == "__main__":
    main()
