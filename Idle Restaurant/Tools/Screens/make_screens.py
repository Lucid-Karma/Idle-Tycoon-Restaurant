# The cafe's two little screens (the kitchen window and the frame on the right wall) scroll a strip sideways
# (Custom/UnlitScrollingTexture). This draws those strips: one seamless 1024x128 tile each, on a soft pastel panel (user:
# no dark LED look), and nothing but the words themselves, drawn as a little piece of lettering - a gradient fill, a
# thick outline, a soft shadow, and the theme's own touch on the letters (user, 2026-10-28: "no tiny faint marks beside
# the words: either the lettering itself is artistic, or it's only a picture"):
#   default    "beyzosh.com"        berry on white outline
#   halloween  "CHIBI BOORGER"      orange comic letters, deep purple outline, dripping
#   winter     "HO HO HO!"          blue letters, white outline, snow lying on top
#   valentine  only hearts           big and small, glossy
# usage: python make_screens.py <out dir>
import math
import random
import sys
from PIL import Image, ImageDraw, ImageFilter, ImageFont, ImageChops

OUT = sys.argv[1] if len(sys.argv) > 1 else '.'
FONTS = r'C:\Users\Beyzanur\Desktop\Projects\UnityProjectsIDo\SERIOUS\Idle Restaurant\Idle-Tycoon-Restaurant\Idle Restaurant\Assets\Project\[GAME]\Graphics\Font'
FREDOKA = FONTS + r'\Fredoka\Fredoka-SemiBold.ttf'
BANGERS = FONTS + r'\Bangers\Bangers-Regular.ttf'
W, H, S = 1024, 128, 4   # drawn 4x larger, then scaled down (smooth edges)
w, h = W * S, H * S


def tiled_text_mask(text, font, gap):
    # the words repeated along the strip so the tile wraps seamlessly, as a mask
    mask = Image.new('L', (w, h), 0)
    d = ImageDraw.Draw(mask)
    tw = d.textlength(text, font=font)
    count = max(1, round(w / (tw + gap)))           # as many as fit with a fair gap (the screens show only part of it)
    while count > 1 and w / count < tw + h * 0.45:
        count -= 1
    unit = w / count
    for i in range(count):
        d.text((i * unit + unit / 2, h * 0.5), text, font=font, fill=255, anchor='mm')
    return mask


def gradient(top, bottom):
    g = Image.new('RGB', (1, h))
    for y in range(h):
        t = y / (h - 1)
        g.putpixel((0, y), tuple(int(a + (b - a) * t) for a, b in zip(top, bottom)))
    return g.resize((w, h))


def grow(mask, px):
    # dilate by about px (repeated 3x3 max filters on a quarter-size copy, then back up: fast and round enough)
    small = mask.resize((w // 4, h // 4), Image.BILINEAR)
    for _ in range(max(1, px // 4)):
        small = small.filter(ImageFilter.MaxFilter(3))
    big = small.resize((w, h), Image.BILINEAR).filter(ImageFilter.GaussianBlur(2))
    return big.point(lambda v: 255 if v > 100 else int(v * 2.5))


def down(mask, px):
    # moved down by px without wrapping round (the strip's top and bottom rows must stay the bare panel)
    out = Image.new('L', (w, h), 0)
    out.paste(mask.crop((0, 0, w, h - px)), (0, px))
    return out


def compose(bg, fill_top, fill_bottom, outline, outline_px, mask, shadow=(0, 0, 0), extra=None):
    img = Image.new('RGB', (w, h), bg)
    out = grow(mask, outline_px)
    # a soft shadow under it all
    sh = out.filter(ImageFilter.GaussianBlur(10)).point(lambda v: int(v * 0.35))
    sh = down(sh, 14)
    img.paste(Image.new('RGB', (w, h), shadow), (0, 0), sh)
    img.paste(Image.new('RGB', (w, h), outline), (0, 0), out)
    img.paste(gradient(fill_top, fill_bottom), (0, 0), mask)
    # a light rim along the top of each letter
    rim = ImageChops.subtract(mask, down(mask, 10)).filter(ImageFilter.GaussianBlur(2)).point(lambda v: int(v * 0.45))
    img.paste(Image.new('RGB', (w, h), (255, 255, 255)), (0, 0), rim)
    if extra:
        extra(img)
    return img


def columns_edges(mask, step):
    # for every `step` columns: the lowest and highest letter pixel (bottom/top edges), or None
    px = mask.load()
    edges = []
    for x in range(0, w, step):
        ys = [y for y in range(0, h, 2) if px[x, y] > 128]
        edges.append((x, (min(ys), max(ys)) if ys else None))
    return edges


def save(img, name):
    img = img.resize((W, H), Image.LANCZOS)
    path = f'{OUT}/screen_{name}.png'
    img.save(path, optimize=True)
    print(path, round(len(open(path, 'rb').read()) / 1024, 1), 'KB')


rng = random.Random(3)

# ---- default: beyzosh.com ----
font = ImageFont.truetype(FREDOKA, int(h * 0.52))
m = tiled_text_mask('beyzosh.com', font, h * 1.4)
save(compose((250, 222, 230), (232, 120, 168), (190, 78, 132), (255, 255, 255), 34, m, shadow=(150, 70, 110)), 'default')

# ---- halloween: CHIBI BOORGER, dripping ----
font = ImageFont.truetype(BANGERS, int(h * 0.6))
m = tiled_text_mask('CHIBI BOORGER', font, h * 1.2)
d = ImageDraw.Draw(m)
for x, e in columns_edges(m, 6):
    if e is None or rng.random() > 0.07:
        continue
    top, bottom = e
    length = rng.uniform(0.08, 0.2) * h
    r = rng.uniform(0.022, 0.034) * h
    d.rounded_rectangle([x - r * 0.7, bottom - r, x + r * 0.7, bottom + length], radius=r * 0.7, fill=255)
    d.ellipse([x - r, bottom + length - r, x + r, bottom + length + r], fill=255)
save(compose((232, 222, 244), (255, 178, 70), (236, 106, 30), (70, 32, 92), 30, m, shadow=(60, 30, 80)), 'halloween')

# ---- winter: HO HO HO!, snow on top ----
font = ImageFont.truetype(FREDOKA, int(h * 0.56))
m = tiled_text_mask('HO HO HO!', font, h * 1.4)
caps = Image.new('L', (w, h), 0)
dc = ImageDraw.Draw(caps)
for x, e in columns_edges(m, 12):
    if e is None:
        continue
    top = e[0]
    r = rng.uniform(0.022, 0.032) * h
    dc.ellipse([x - r * 1.8, top - r * 1.25, x + r * 1.8, top + r * 0.2], fill=255)
caps = ImageChops.multiply(caps, grow(m, 30))     # only where there is a letter under it


def snow(img):
    img.paste(Image.new('RGB', (w, h), (186, 214, 240)), (0, 0), down(caps, 6).filter(ImageFilter.GaussianBlur(3)))   # its shade
    img.paste(Image.new('RGB', (w, h), (255, 255, 255)), (0, 0), caps.filter(ImageFilter.GaussianBlur(2)))


save(compose((224, 237, 248), (124, 172, 226), (72, 118, 186), (36, 64, 112), 20, m, shadow=(70, 100, 150), extra=snow), 'winter')


# ---- valentine: hearts only ----
def heart(d, cx, cy, r, fill):
    d.ellipse([cx - r, cy - r * 0.75, cx, cy + r * 0.25], fill=fill)
    d.ellipse([cx, cy - r * 0.75, cx + r, cy + r * 0.25], fill=fill)
    d.polygon([(cx - r * 0.97, cy - r * 0.15), (cx + r * 0.97, cy - r * 0.15), (cx, cy + r * 1.05)], fill=fill)


m = Image.new('L', (w, h), 0)
d = ImageDraw.Draw(m)
count = 8
for i in range(count + 1):
    heart(d, i * w / count, h * 0.48, h * (0.27 if i % 2 == 0 else 0.19), 255)
save(compose((251, 220, 230), (240, 110, 150), (205, 60, 110), (255, 255, 255), 28, m, shadow=(160, 60, 100)), 'valentine')
