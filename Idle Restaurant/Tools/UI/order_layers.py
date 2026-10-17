"""The burger layers a customer's order is drawn with (UIOrderTicket): flat, chunky shapes with the UI's ink
outline, so a stack of them reads as "that burger" in a bubble a few dozen pixels wide - colour carries it:
brown patty, yellow cheese, green lettuce, red tomato, lilac onion. Writes Assets/Project/[GAME]/Graphics/
Sprites/Orders/layer_<name>.png (bun_top, bun_bottom, burger, cheese, lettuce, tomato, onion), 4x supersampled.
Needs Pillow."""
import math, os
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "..", "..", "Assets", "Project", "[GAME]", "Graphics", "Sprites", "Orders")
S = 4                      # supersampling
W = 128                    # every layer is drawn on the same width, so they stack centred
INK = (16, 38, 58, 255)
LINE = 5                   # outline, final pixels

BUN = (238, 168, 92, 255)
BUN_LIGHT = (250, 205, 140, 255)
SEED = (255, 246, 228, 255)
PATTY = (122, 72, 52, 255)
PATTY_LIGHT = (156, 98, 72, 255)
CHEESE = (246, 200, 72, 255)
LETTUCE = (118, 196, 96, 255)
LETTUCE_DARK = (82, 160, 72, 255)
TOMATO = (233, 96, 86, 255)
TOMATO_LIGHT = (246, 150, 136, 255)
ONION = (230, 206, 240, 255)
ONION_DARK = (186, 150, 210, 255)


def canvas(h):
    img = Image.new("RGBA", (W * S, h * S), (0, 0, 0, 0))
    return img, ImageDraw.Draw(img)


def save(img, name):
    img = img.resize((img.width // S, img.height // S), Image.LANCZOS)
    img.save(os.path.join(OUT, f"layer_{name}.png"))


def rrect(d, box, r, fill):
    x0, y0, x1, y1 = [v * S for v in box]
    d.rounded_rectangle((x0, y0, x1, y1), radius=r * S, fill=INK)
    i = LINE * S
    d.rounded_rectangle((x0 + i, y0 + i, x1 - i, y1 - i), radius=max(1, (r - LINE)) * S, fill=fill)


def bun_top():
    h = 58
    img, d = canvas(h)
    d.pieslice((2 * S, 2 * S, (W - 2) * S, (2 * h - 10) * S), 180, 360, fill=INK)
    d.rectangle((2 * S, (h - 12) * S, (W - 2) * S, (h - 2) * S), fill=INK)
    i = LINE
    d.pieslice(((2 + i) * S, (2 + i) * S, (W - 2 - i) * S, (2 * h - 10 - i) * S), 180, 360, fill=BUN)
    d.rectangle(((2 + i) * S, (h - 12) * S, (W - 2 - i) * S, (h - 2 - i) * S), fill=BUN)
    d.ellipse((24 * S, 16 * S, 54 * S, 30 * S), fill=BUN_LIGHT)          # shine
    for x, y in ((44, 30), (66, 20), (86, 32), (62, 40), (34, 42), (94, 44)):
        d.ellipse(((x - 4) * S, (y - 2) * S, (x + 4) * S, (y + 3) * S), fill=SEED)
    save(img, "bun_top")


def bun_bottom():
    img, d = canvas(30)
    rrect(d, (4, 2, W - 4, 28), 10, BUN)
    save(img, "bun_bottom")


def patty():
    img, d = canvas(30)
    rrect(d, (2, 2, W - 2, 28), 12, PATTY)
    for x in (24, 46, 70, 94):
        d.line(((x - 6) * S, 12 * S, (x + 6) * S, 12 * S), fill=PATTY_LIGHT, width=3 * S)
    save(img, "burger")


def cheese():
    img, d = canvas(34)
    pts = [(0, 4), (W, 4), (W, 16), (82, 16), (70, 32), (58, 16), (0, 16)]
    d.polygon([(x * S, y * S) for x, y in pts], fill=INK)
    i = LINE
    inner = [(i, 4 + i), (W - i, 4 + i), (W - i, 16 - i // 2), (80, 16 - i // 2), (70, 32 - i - 2), (60, 16 - i // 2), (i, 16 - i // 2)]
    d.polygon([(x * S, y * S) for x, y in inner], fill=CHEESE)
    save(img, "cheese")


def lettuce():
    h = 26
    img, d = canvas(h)
    def wave(inset, colour):
        top = []
        for k in range(0, W + 1, 2):
            top.append((k, 6 + 4 * math.sin(k / 9.0) + inset))
        bottom = []
        for k in range(W, -1, -2):
            bottom.append((k, h - 4 + 3 * math.sin(k / 7.0 + 1) - inset))
        pts = [(max(inset, min(W - inset, x)), y) for x, y in top + bottom]
        d.polygon([(x * S, y * S) for x, y in pts], fill=colour)
    wave(0, INK)
    wave(LINE, LETTUCE)
    d.line((14 * S, 13 * S, (W - 14) * S, 13 * S), fill=LETTUCE_DARK, width=2 * S)
    save(img, "lettuce")


def tomato():
    img, d = canvas(24)
    rrect(d, (8, 2, W - 8, 22), 9, TOMATO)
    for x in (34, 64, 94):
        d.ellipse(((x - 6) * S, 9 * S, (x + 6) * S, 15 * S), fill=TOMATO_LIGHT)
    save(img, "tomato")


def onion():
    img, d = canvas(22)
    rrect(d, (10, 2, W - 10, 20), 9, ONION)
    d.line((24 * S, 11 * S, (W - 24) * S, 11 * S), fill=ONION_DARK, width=3 * S)
    save(img, "onion")


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    for f in (bun_top, bun_bottom, patty, cheese, lettuce, tomato, onion):
        f()
    print("wrote layers to", os.path.abspath(OUT))
