"""Lays out the Chibi Burger Cafe store covers from renders of the game's own models (see CoverRenderer.cs):
  parts/cafe.png            the cafe as the game camera sees it (transparent background)
  parts/<food>.png          single ingredients (CoverRenderer.RenderFood)
  GameTitle.png / keyart_rush.png   the logo and the chef with his Chaos Burger (already in the project)
The only text on a cover is the logo. usage: python compose_covers.py <parts dir> <out dir>
Needs Pillow + numpy."""
import math, os, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

PARTS, OUT = sys.argv[1], sys.argv[2]
SPRITES = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Assets", "Project", "[GAME]", "Graphics", "Sprites")
SS = 2  # supersampling
PINK_TOP, PINK_BOT = (253, 204, 220), (246, 172, 199)
INK_SHADOW = (170, 70, 110)


def load_image(im):
    a = np.array(im)[:, :, 3]
    ys, xs = np.where(a > 8)
    return im.crop((xs.min(), ys.min(), xs.max() + 1, ys.max() + 1))


def load(path):
    return load_image(Image.open(path).convert("RGBA"))


def unhaze(im):
    """Renders made with post-processing on carry a faint haze (the white and black backgrounds differ by a little
    less than 1): the corner alpha is that haze. Re-matte: alpha' = (a - a0) / (1 - a0), colour from the premultiplied
    (black-render) value. KeyArtRenderer.RenderMatted now calibrates this itself; older renders need it."""
    arr = np.asarray(im).astype(np.float32) / 255.0
    a = arr[:, :, 3]
    a0 = float(np.mean([a[0, 0], a[0, -1], a[-1, 0], a[-1, -1]]))
    if a0 < 0.004:
        return im
    rgb = arr[:, :, :3]
    lin = np.where(rgb <= 0.04045, rgb / 12.92, ((rgb + 0.055) / 1.055) ** 2.4) * a[:, :, None]   # premultiplied, linear
    a2 = np.clip((a - a0) / (1 - a0), 0, 1)
    c = np.clip(lin / np.maximum(a2, 1e-3)[:, :, None], 0, 1)
    c = np.where(c <= 0.0031308, c * 12.92, 1.055 * c ** (1 / 2.4) - 0.055)
    out = np.dstack([c, a2])
    return Image.fromarray((out * 255 + 0.5).astype(np.uint8), "RGBA")


def part(name):
    im = Image.open(os.path.join(PARTS, name + ".png")).convert("RGBA")
    return load_image(unhaze(im))


def fit(im, width=None, height=None):
    if width is not None:
        height = round(im.height * width / im.width)
    else:
        width = round(im.width * height / im.height)
    return im.resize((width, height), Image.LANCZOS)


def background(w, h, cx, cy, rays=18):
    """Soft pink gradient, a light sunburst from (cx, cy), a glow behind the subject."""
    t = np.linspace(0, 1, h)[:, None, None]
    base = np.array(PINK_TOP) * (1 - t) + np.array(PINK_BOT) * t
    img = Image.fromarray(np.repeat(base, w, axis=1).astype(np.uint8), "RGB").convert("RGBA")
    layer = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    R = math.hypot(w, h)
    for i in range(rays):
        a0 = (i * 2) * math.pi / rays + 0.12
        a1 = a0 + math.pi / rays
        d.polygon([(cx, cy), (cx + R * math.cos(a0), cy + R * math.sin(a0)), (cx + R * math.cos(a1), cy + R * math.sin(a1))], fill=(255, 246, 249, 34))
    img.alpha_composite(layer.filter(ImageFilter.GaussianBlur(1.2 * SS)))
    glow = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    ImageDraw.Draw(glow).ellipse((cx - w * 0.5, cy - h * 0.42, cx + w * 0.5, cy + h * 0.42), fill=(255, 240, 245, 130))
    img.alpha_composite(glow.filter(ImageFilter.GaussianBlur(w * 0.09)))
    return img


def streak(canvas, x, y, length, angle, width=14 * SS, alpha=235):
    layer = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    dx, dy = math.cos(math.radians(angle)) * length / 2, math.sin(math.radians(angle)) * length / 2
    d.line((x - dx, y - dy, x + dx, y + dy), fill=(255, 255, 255, alpha), width=width)
    r = width / 2
    for px, py in ((x - dx, y - dy), (x + dx, y + dy)):
        d.ellipse((px - r, py - r, px + r, py + r), fill=(255, 255, 255, alpha))
    canvas.alpha_composite(layer)


def paste(canvas, im, x, y, rot=0, shadow=0.38, lift=18, blur=16):
    """Paste `im` with its top-left near (x, y), rotated, with a soft drop shadow on the pink."""
    if rot:
        im = im.rotate(rot, resample=Image.BICUBIC, expand=True)
    if shadow:
        a = im.split()[3].point(lambda v: int(v * shadow))
        sh = Image.new("RGBA", im.size, INK_SHADOW + (0,))
        sh.putalpha(a)
        pad = blur * 3
        big = Image.new("RGBA", (im.width + pad * 2, im.height + pad * 2), (0, 0, 0, 0))
        big.paste(sh, (pad, pad))
        big = big.filter(ImageFilter.GaussianBlur(blur))
        canvas.alpha_composite(big, (int(x - pad), int(y - pad + lift)))
    canvas.alpha_composite(im, (int(x), int(y)))


def foods(canvas, spec):
    for name, cx, cy, width, rot in spec:
        im = fit(part(name), width=int(width * SS))
        if rot:
            im = im.rotate(rot, resample=Image.BICUBIC, expand=True)
        paste(canvas, im, cx * SS - im.width / 2, cy * SS - im.height / 2, 0, lift=14 * SS, blur=10 * SS)


def logo(canvas, cx, top, width, rot=-2.5):
    im = fit(load(os.path.join(SPRITES, "GameTitle.png")), width=int(width * SS))
    paste(canvas, im, cx * SS - im.width / 2, top * SS, rot, shadow=0.35, lift=8 * SS, blur=8 * SS)


def cafe_with_shadow(canvas, width, cx, top):
    """The diorama floats: a soft shadow under it, offset down, like the title screen's chef."""
    cafe = fit(part("cafe"), width=int(width * SS))
    x = cx * SS - cafe.width / 2
    y = top * SS
    sh = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
    ImageDraw.Draw(sh).polygon([
        (x + cafe.width * 0.02, y + cafe.height * 0.74), (x + cafe.width * 0.55, y + cafe.height * 1.04),
        (x + cafe.width * 0.98, y + cafe.height * 0.78), (x + cafe.width * 0.48, y + cafe.height * 0.50)], fill=INK_SHADOW + (95,))
    canvas.alpha_composite(sh.filter(ImageFilter.GaussianBlur(26 * SS)))
    canvas.alpha_composite(cafe, (int(x), int(y)))
    return cafe


def save(canvas, name, size):
    out = canvas.convert("RGB").resize(size, Image.LANCZOS)
    path = os.path.join(OUT, name)
    out.save(path, optimize=True)
    print(path, out.size)


def landscape():
    W, H = 1920, 1080
    c = background(W * SS, H * SS, int(W * 0.40 * SS), int(H * 0.55 * SS))
    for x, y, l, a in [(52, 600, 100, -28), (1010, 1030, 90, -30), (1885, 420, 80, -28), (960, 40, 70, -30), (1330, 1035, 70, -28)]:
        streak(c, x * SS, y * SS, l * SS, a)
    foods(c, [("lettuce", 95, 255, 190, -15), ("cheese", 770, 85, 250, -18), ("bun", 330, 100, 260, 10),
              ("tomatoSlice", 120, 940, 230, 8), ("tomatoSlice", 1580, 975, 230, -10), ("patty", 1790, 720, 240, 8),
              ("onionSlice", 1620, 575, 140, -12), ("bottomBun", 1850, 965, 190, 14)])
    cafe_with_shadow(c, 1430, 735, 268)
    logo(c, 1500, 40, 700)
    save(c, "chibi-burger-cafe_cover_landscape_1920x1080.png", (W, H))


def square():
    W = H = 800
    c = background(W * SS, H * SS, int(W * 0.5 * SS), int(H * 0.62 * SS), rays=16)
    for x, y, l, a in [(40, 470, 70, -28), (760, 560, 80, -30), (330, 770, 60, -30), (610, 28, 60, -28)]:
        streak(c, x * SS, y * SS, l * SS, a, width=10 * SS)
    foods(c, [("bun", 85, 175, 125, 12), ("cheese", 735, 170, 125, -16), ("lettuce", 725, 345, 85, 18),
              ("tomatoSlice", 82, 715, 125, 8), ("patty", 722, 748, 125, -8)])
    cafe_with_shadow(c, 790, 400, 335)
    logo(c, 400, 18, 540)
    save(c, "chibi-burger-cafe_cover_square_800x800.png", (W, H))


def portrait():
    W, H = 800, 1200
    c = background(W * SS, H * SS, int(W * 0.5 * SS), int(H * 0.64 * SS), rays=16)
    for x, y, l, a in [(45, 500, 90, -28), (748, 470, 90, -30), (60, 900, 80, -30), (300, 1175, 80, -28)]:
        streak(c, x * SS, y * SS, l * SS, a, width=11 * SS)
    cafe_with_shadow(c, 810, 400, 745)
    chef = fit(load(os.path.join(SPRITES, "KeyArt", "keyart_rush.png")), height=int(840 * SS))
    paste(c, chef, 405 * SS, 345 * SS, 0, shadow=0.35, lift=14 * SS, blur=12 * SS)
    foods(c, [("bun", 120, 410, 140, 10), ("cheese", 700, 395, 120, -16), ("lettuce", 100, 585, 100, 18),
              ("onionSlice", 215, 520, 100, -10), ("tomatoSlice", 105, 720, 120, 8), ("patty", 95, 1125, 130, -10)])
    logo(c, 400, 28, 620)
    save(c, "chibi-burger-cafe_cover_portrait_800x1200.png", (W, H))


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    landscape()
    square()
    portrait()
