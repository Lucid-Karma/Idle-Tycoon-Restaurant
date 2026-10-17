"""Store covers from real renders of the cafe (Assets/Editor/StoreCoverRenderer.cs) with the game's own logo on top.

usage: python compose_store.py <landscape.png> <portrait.png> <square.png> <out dir>
  the inputs come from StoreCoverRenderer.RenderAll(<dir>) (bridge scall), twice the size of each cover; outputs:
  chibi-burger-cafe_cover_landscape_1920x1080.png, _portrait_800x1200.png, _square_800x800.png

Third version (2026-10-14). The covers before this one had been painted by an image generator from our renders and
showed things the game does not have - fries, drinks, bunny and cat customers, a menu board, pendant lamps, a second
cook - and the user asked for them again with only what is in the game. Everything in the picture is now rendered
from the game's own scene and models; the only thing added here is the title (GameTitle.png, the game's logo, with
the ink outline of the second version) and a soft light behind it so it holds on the pink wall. Needs Pillow + numpy.
"""
import os, sys
import numpy as np
from PIL import Image, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
SPRITES = os.path.join(HERE, "..", "..", "Assets", "Project", "[GAME]", "Graphics", "Sprites")
INK = (16, 38, 58)


def crop_alpha(im):
    a = np.array(im)[:, :, 3]
    ys, xs = np.where(a > 8)
    return im.crop((xs.min(), ys.min(), xs.max() + 1, ys.max() + 1))


def outlined(im, px, color=INK):
    """`im` with a solid outline `px` wide around its alpha (sticker style)."""
    pad = px + 2
    big = Image.new("RGBA", (im.width + pad * 2, im.height + pad * 2), (0, 0, 0, 0))
    big.paste(im, (pad, pad))
    grown = big.split()[3]
    step = 0
    while step < px:
        k = min(px - step, 6)
        grown = grown.filter(ImageFilter.MaxFilter(k * 2 + 1))
        step += k
    grown = grown.filter(ImageFilter.GaussianBlur(0.8)).point(lambda v: 255 if v > 110 else int(v * 2.3))
    out = Image.new("RGBA", big.size, color + (0,))
    out.putalpha(grown)
    out.alpha_composite(big)
    return out


def logo_art():
    """GameTitle.png is 512 px: scaled up 4x and its alpha re-sharpened (the letters are flat shapes), then given a
    thick ink outline."""
    im = crop_alpha(Image.open(os.path.join(SPRITES, "GameTitle.png")).convert("RGBA"))
    im = im.resize((im.width * 4, im.height * 4), Image.LANCZOS)
    r, g, b, a = im.split()
    a = a.point(lambda v: 0 if v < 70 else 255 if v > 185 else int((v - 70) / 115 * 255))
    return outlined(Image.merge("RGBA", (r, g, b, a)), 22)


def drop_shadow(canvas, im, x, y, offset, blur, alpha=0.45, color=INK):
    a = im.split()[3].point(lambda v: int(v * alpha))
    sh = Image.new("RGBA", im.size, color + (0,))
    sh.putalpha(a)
    pad = int(blur * 3)
    big = Image.new("RGBA", (im.width + pad * 2, im.height + pad * 2), (0, 0, 0, 0))
    big.paste(sh, (pad, pad))
    canvas.alpha_composite(big.filter(ImageFilter.GaussianBlur(blur)), (int(x - pad + offset[0]), int(y - pad + offset[1])))


def glow(canvas, cx, cy, rx, ry, color=(255, 250, 246), alpha=150):
    """A soft warm-white light behind the title."""
    w, h = canvas.size
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    d = ((xx - cx) / rx) ** 2 + ((yy - cy) / ry) ** 2
    a = np.clip(1.0 - d, 0, 1) ** 1.6 * alpha
    layer = np.zeros((h, w, 4), np.uint8)
    layer[:, :, 0], layer[:, :, 1], layer[:, :, 2] = color
    layer[:, :, 3] = a.astype(np.uint8)
    canvas.alpha_composite(Image.fromarray(layer, "RGBA"))


def vignette(canvas, strength=0.22):
    w, h = canvas.size
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    d = np.sqrt(((xx - w / 2) / (w / 2)) ** 2 + ((yy - h / 2) / (h / 2)) ** 2)
    a = np.clip((d - 0.75) / 0.7, 0, 1) * 255 * strength
    layer = np.zeros((h, w, 4), np.uint8)
    layer[:, :, 0], layer[:, :, 1], layer[:, :, 2] = (60, 20, 50)
    layer[:, :, 3] = a.astype(np.uint8)
    canvas.alpha_composite(Image.fromarray(layer, "RGBA"))


def cover(render, size, logo_width, logo_top, logo_cx=0.5, rot=-3.0):
    W, H = size[0] * 2, size[1] * 2
    canvas = Image.open(render).convert("RGBA").resize((W, H), Image.LANCZOS)
    vignette(canvas)
    logo = logo_art()
    lw = int(logo_width * 2)
    logo = logo.resize((lw, int(logo.height * lw / logo.width)), Image.LANCZOS)
    if rot:
        logo = logo.rotate(rot, resample=Image.BICUBIC, expand=True)
    x, y = logo_cx * W - logo.width / 2, logo_top * 2
    glow(canvas, x + logo.width / 2, y + logo.height / 2, logo.width * 0.75, logo.height * 0.85)
    drop_shadow(canvas, logo, x, y, (0, 14), 12, alpha=0.5)
    canvas.alpha_composite(logo, (int(x), int(y)))
    return canvas.convert("RGB").resize(size, Image.LANCZOS)


if __name__ == "__main__":
    land, port, square, out = sys.argv[1:5]
    os.makedirs(out, exist_ok=True)
    cover(land, (1920, 1080), 820, 22).save(os.path.join(out, "chibi-burger-cafe_cover_landscape_1920x1080.png"), optimize=True)
    cover(port, (800, 1200), 660, 34).save(os.path.join(out, "chibi-burger-cafe_cover_portrait_800x1200.png"), optimize=True)
    cover(square, (800, 800), 540, 18).save(os.path.join(out, "chibi-burger-cafe_cover_square_800x800.png"), optimize=True)
    print("ok")
