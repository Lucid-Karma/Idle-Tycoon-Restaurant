"""Lays out the Chibi Burger Cafe store covers from renders of the game's own models:
  parts/hero.png            the chef, grinning, a giant burger on his head (HeroArtRenderer.cs)
  parts/cafe.png            the cafe as the game camera sees it (CoverRenderer.RenderCafe)
  parts/<food>.png          single ingredients (CoverRenderer.RenderFood)
  GameTitle.png             the logo (already in the project)
The only text on a cover is the logo. usage: python compose_covers.py <parts dir> <out dir>
Needs Pillow + numpy.

Second version. The first covers (the cafe as a small diorama on soft pink) sat at 1.1% click-through on
CrazyGames: next to the Cooking/Simulation thumbnails - saturated, warm reds and yellows, a big face, a huge
title - they were pale and nothing in them read at tile size. Now: one big hero (a face and a burger read at any
size), the title as large as the frame allows with a dark outline, and a teal background with a yellow burst,
which none of the warm-coloured neighbours use. The pink and white checker floor is the game's own kitchen."""
import math, os, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

PARTS, OUT = sys.argv[1], sys.argv[2]
SPRITES = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Assets", "Project", "[GAME]", "Graphics", "Sprites")
SS = 2  # supersampling
TEAL_IN, TEAL_OUT = (64, 214, 204), (8, 104, 112)
YELLOW, YELLOW_IN = (255, 200, 60), (255, 236, 150)
FLOOR_PINK, FLOOR_WHITE = (246, 160, 188), (255, 247, 242)
INK = (16, 38, 58)


def crop_alpha(im):
    a = np.array(im)[:, :, 3]
    ys, xs = np.where(a > 8)
    return im.crop((xs.min(), ys.min(), xs.max() + 1, ys.max() + 1))


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
    return crop_alpha(unhaze(Image.open(os.path.join(PARTS, name + ".png")).convert("RGBA")))


def fit(im, width=None, height=None):
    if width is not None:
        height = round(im.height * width / im.width)
    else:
        width = round(im.width * height / im.height)
    return im.resize((max(1, width), max(1, height)), Image.LANCZOS)


def radial(w, h, cx, cy, inner, outer, reach):
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    t = np.clip(np.hypot(xx - cx, yy - cy) / reach, 0, 1)[:, :, None] ** 1.3
    rgb = np.array(inner) * (1 - t) + np.array(outer) * t
    return Image.fromarray(rgb.astype(np.uint8), "RGB").convert("RGBA")


def rays(canvas, cx, cy, count, color, alpha, turn=0.0):
    w, h = canvas.size
    layer = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    R = math.hypot(w, h)
    for i in range(count):
        a0 = (i * 2) * math.pi / count + turn
        a1 = a0 + math.pi / count
        d.polygon([(cx, cy), (cx + R * math.cos(a0), cy + R * math.sin(a0)), (cx + R * math.cos(a1), cy + R * math.sin(a1))], fill=color + (alpha,))
    canvas.alpha_composite(layer.filter(ImageFilter.GaussianBlur(1.0 * SS)))


def burst(canvas, cx, cy, r, points=14, inner=0.80):
    """A comic-book starburst in yellow, lighter towards the middle, with a dark outline."""
    w, h = canvas.size
    pts = []
    for i in range(points * 2):
        a = i * math.pi / points - math.pi / 2
        rr = r if i % 2 == 0 else r * inner
        pts.append((cx + rr * math.cos(a), cy + rr * math.sin(a)))
    shadow = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    ImageDraw.Draw(shadow).polygon([(x + r * 0.03, y + r * 0.05) for x, y in pts], fill=INK + (70,))
    canvas.alpha_composite(shadow.filter(ImageFilter.GaussianBlur(r * 0.04)))
    mask = Image.new("L", (w, h), 0)
    ImageDraw.Draw(mask).polygon(pts, fill=255)
    fill = radial(w, h, cx, cy, YELLOW_IN, YELLOW, r)
    outline = Image.new("RGBA", (w, h), INK + (255,))
    grown = mask.filter(ImageFilter.MaxFilter(int(r * 0.022) * 2 + 1))
    canvas.paste(outline, (0, 0), grown)
    canvas.paste(fill, (0, 0), mask)


def floor(canvas, horizon, top, cx, tile, tilt=0.0):
    """The kitchen's pink and white checker floor, in perspective from `top` down, fading out towards the top."""
    w, h = canvas.size
    ys, xs = np.mgrid[top:h, 0:w].astype(np.float32)
    depth = tile * 6.0 / np.maximum(ys - horizon, 1.0)          # distance into the picture
    wx = (xs - cx) * depth / tile * 0.9
    wz = depth * 9.0
    c, s = math.cos(tilt), math.sin(tilt)
    u, v = wx * c - wz * s, wx * s + wz * c
    check = ((np.floor(u) + np.floor(v)) % 2)[:, :, None]
    rgb = np.array(FLOOR_PINK) * check + np.array(FLOOR_WHITE) * (1 - check)
    fade = np.clip((ys - top) / (h - top) * 2.2, 0, 1)
    img = Image.fromarray(np.dstack([rgb, (fade * 255)]).astype(np.uint8), "RGBA")
    # A soft teal line where the floor meets the wall would be hard; a gentle shade near the top of the floor instead.
    shade = np.clip(1 - (ys - top) / (h - top) * 3, 0, 1) * 0.18
    arr = np.asarray(img).astype(np.float32)
    arr[:, :, :3] *= (1 - shade)[:, :, None]
    canvas.alpha_composite(Image.fromarray(arr.astype(np.uint8), "RGBA"), (0, top))


def drop_shadow(canvas, im, x, y, offset, blur, alpha=0.45, color=INK):
    a = im.split()[3].point(lambda v: int(v * alpha))
    sh = Image.new("RGBA", im.size, color + (0,))
    sh.putalpha(a)
    pad = int(blur * 3)
    big = Image.new("RGBA", (im.width + pad * 2, im.height + pad * 2), (0, 0, 0, 0))
    big.paste(sh, (pad, pad))
    canvas.alpha_composite(big.filter(ImageFilter.GaussianBlur(blur)), (int(x - pad + offset[0]), int(y - pad + offset[1])))


def outlined(im, px, color=INK):
    """`im` with a solid outline `px` wide around its alpha (sticker style)."""
    pad = px + 2
    big = Image.new("RGBA", (im.width + pad * 2, im.height + pad * 2), (0, 0, 0, 0))
    big.paste(im, (pad, pad))
    a = big.split()[3]
    grown = a
    step = 0
    while step < px:                       # MaxFilter sizes are limited; grow in steps
        k = min(px - step, 6)
        grown = grown.filter(ImageFilter.MaxFilter(k * 2 + 1))
        step += k
    grown = grown.filter(ImageFilter.GaussianBlur(0.8)).point(lambda v: 255 if v > 110 else int(v * 2.3))
    out = Image.new("RGBA", big.size, color + (0,))
    out.putalpha(grown)
    out.alpha_composite(big)
    return out


def hero(canvas, cx, bottom, height, name="hero"):
    """cx is where his body stands; the picture also holds the tomato flying off to his left (screen right)."""
    im = fit(part(name), height=int(height * SS))
    x, y = cx * SS - im.width * 0.40, bottom * SS - im.height
    fw = im.width * 0.62
    # Contact shadow on the floor under his feet.
    sh = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
    ImageDraw.Draw(sh).ellipse((cx * SS - fw / 2, bottom * SS - fw * 0.10, cx * SS + fw / 2, bottom * SS + fw * 0.08), fill=INK + (110,))
    canvas.alpha_composite(sh.filter(ImageFilter.GaussianBlur(10 * SS)))
    drop_shadow(canvas, im, x, y, (14 * SS, 10 * SS), 14 * SS, alpha=0.30)
    canvas.alpha_composite(im, (int(x), int(y)))
    return im, x, y


def streak(canvas, x, y, length, angle, width=12, alpha=230):
    layer = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    dx, dy = math.cos(math.radians(angle)) * length / 2, math.sin(math.radians(angle)) * length / 2
    d.line((x - dx, y - dy, x + dx, y + dy), fill=(255, 255, 255, alpha), width=int(width))
    r = width / 2
    for px, py in ((x - dx, y - dy), (x + dx, y + dy)):
        d.ellipse((px - r, py - r, px + r, py + r), fill=(255, 255, 255, alpha))
    canvas.alpha_composite(layer)


def foods(canvas, spec):
    for name, cx, cy, width, rot in spec:
        im = fit(part(name), width=int(width * SS))
        if rot:
            im = im.rotate(rot, resample=Image.BICUBIC, expand=True)
        x, y = cx * SS - im.width / 2, cy * SS - im.height / 2
        drop_shadow(canvas, im, x, y, (8 * SS, 12 * SS), 8 * SS, alpha=0.35)
        canvas.alpha_composite(im, (int(x), int(y)))


_logo = None


def logo_art():
    """GameTitle.png is 512 px: scaled up 4x and its alpha re-sharpened (the letters are flat shapes), then given
    a thick ink outline so the pink and white title holds on any background."""
    global _logo
    if _logo is None:
        im = crop_alpha(Image.open(os.path.join(SPRITES, "GameTitle.png")).convert("RGBA"))
        im = im.resize((im.width * 4, im.height * 4), Image.LANCZOS)
        r, g, b, a = im.split()
        a = a.point(lambda v: 0 if v < 70 else 255 if v > 185 else int((v - 70) / 115 * 255))
        im = Image.merge("RGBA", (r, g, b, a))
        _logo = outlined(im, 22)
    return _logo


def logo(canvas, cx, top, width, rot=-3.0):
    im = fit(logo_art(), width=int(width * SS))
    if rot:
        im = im.rotate(rot, resample=Image.BICUBIC, expand=True)
    x, y = cx * SS - im.width / 2, top * SS
    drop_shadow(canvas, im, x, y, (0, 12 * SS), 10 * SS, alpha=0.55)
    canvas.alpha_composite(im, (int(x), int(y)))


def save(canvas, name, size):
    out = canvas.convert("RGB").resize(size, Image.LANCZOS)
    path = os.path.join(OUT, name)
    out.save(path, optimize=True)
    print(path, out.size)


def landscape():
    W, H = 1920, 1080
    hx = 560
    c = radial(W * SS, H * SS, hx * SS, 430 * SS, TEAL_IN, TEAL_OUT, 1500 * SS)
    rays(c, hx * SS, 430 * SS, 20, (255, 255, 255), 26, 0.1)
    floor(c, 560 * SS, 800 * SS, hx * SS, 80 * SS, tilt=0.35)
    burst(c, hx * SS, 470 * SS, 470 * SS, points=16)
    for x, y, l, a in [(130, 210, 110, -30), (1010, 150, 90, -28), (1050, 860, 100, -30), (95, 760, 80, -28)]:
        streak(c, x * SS, y * SS, l * SS, a, width=13 * SS)
    foods(c, [("cheese", 150, 300, 200, -20), ("lettuce", 930, 760, 150, 25), ("tomatoSlice", 140, 610, 180, 12),
              ("onionSlice", 975, 560, 120, -15)])
    hero(c, hx, 1050, 1010)
    logo(c, 1430, 120, 900)
    # The cafe the chef runs, under the title: says "restaurant game" in a glance.
    cafe = fit(part("cafe"), width=int(820 * SS))
    cx, cy = 1440 * SS - cafe.width / 2, 545 * SS
    drop_shadow(c, cafe, cx, cy, (0, 22 * SS), 18 * SS, alpha=0.45)
    c.alpha_composite(cafe, (int(cx), int(cy)))
    save(c, "chibi-burger-cafe_cover_landscape_1920x1080.png", (W, H))


def square():
    W = H = 800
    hx = 400
    c = radial(W * SS, H * SS, hx * SS, 470 * SS, TEAL_IN, TEAL_OUT, 700 * SS)
    rays(c, hx * SS, 470 * SS, 18, (255, 255, 255), 26, 0.1)
    floor(c, 520 * SS, 640 * SS, hx * SS, 46 * SS, tilt=0.35)
    burst(c, hx * SS, 530 * SS, 275 * SS, points=14)
    for x, y, l, a in [(60, 330, 70, -30), (745, 400, 70, -28), (700, 700, 60, -30)]:
        streak(c, x * SS, y * SS, l * SS, a, width=9 * SS)
    foods(c, [("cheese", 110, 420, 125, -20), ("lettuce", 715, 380, 105, 25), ("tomatoSlice", 690, 600, 120, 12),
              ("onionSlice", 110, 640, 95, -15)])
    hero(c, hx, 792, 480)
    logo(c, 400, 12, 600)
    save(c, "chibi-burger-cafe_cover_square_800x800.png", (W, H))


def portrait():
    W, H = 800, 1200
    hx = 400
    c = radial(W * SS, H * SS, hx * SS, 700 * SS, TEAL_IN, TEAL_OUT, 900 * SS)
    rays(c, hx * SS, 700 * SS, 18, (255, 255, 255), 26, 0.1)
    floor(c, 820 * SS, 960 * SS, hx * SS, 56 * SS, tilt=0.35)
    burst(c, hx * SS, 720 * SS, 360 * SS, points=14)
    for x, y, l, a in [(70, 470, 80, -30), (740, 560, 80, -28), (90, 1000, 70, -30), (720, 1090, 70, -28)]:
        streak(c, x * SS, y * SS, l * SS, a, width=10 * SS)
    foods(c, [("cheese", 115, 610, 140, -20), ("lettuce", 690, 450, 125, 25), ("tomatoSlice", 690, 760, 135, 12),
              ("onionSlice", 110, 850, 110, -15)])
    hero(c, hx, 1160, 830)
    logo(c, 400, 26, 720)
    save(c, "chibi-burger-cafe_cover_portrait_800x1200.png", (W, H))
    # The portrait preview video (1080x1620) opens on this cover: same layout, video size.
    save(c, "video/cover_portrait_1080x1620.png", (1080, 1620))


if __name__ == "__main__":
    os.makedirs(os.path.join(OUT, "video"), exist_ok=True)
    landscape()
    square()
    portrait()
