"""The burger layers a customer's order is drawn with (UIOrderTicket): flat, chunky shapes with the UI's ink
outline, so a stack of them reads as "that burger" in a bubble a few dozen pixels wide - colour carries it:
brown patty, yellow cheese, green lettuce, red tomato, lilac onion. Writes Assets/Project/[GAME]/Graphics/
Sprites/Orders/layer_<name>.png (bun_top, bun_bottom, burger, cheese, lettuce, tomato, onion, bacon) and side_fries.png
(the carton by an order with fries on the side), 4x supersampled. Pass names to write only those ("bacon fries").
Needs Pillow."""
import math, os, sys
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
BACON = (196, 78, 70, 255)
BACON_FAT = (250, 222, 200, 255)
FRY = (252, 206, 84, 255)
FRY_DARK = (232, 168, 56, 255)
CARTON = (226, 62, 62, 255)
CARTON_LIGHT = (255, 246, 236, 255)


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


def bacon():
    """A wavy rasher: red meat with a stripe of fat down the middle."""
    h = 26
    img, d = canvas(h)
    def wave(k):
        return 3.5 * math.sin(k / 11.0)
    def band(inset, colour):
        top = [(k, 5 + wave(k) + inset) for k in range(0, W + 1, 2)]
        bottom = [(k, h - 5 + wave(k) - inset) for k in range(W, -1, -2)]
        pts = [(max(inset, min(W - inset, x)), y) for x, y in top + bottom]
        d.polygon([(x * S, y * S) for x, y in pts], fill=colour)
    band(0, INK)
    band(LINE - 1, BACON)
    mid = h / 2
    top = [(k, mid - 1.9 + wave(k)) for k in range(14, W - 13, 2)]
    bottom = [(k, mid + 1.9 + wave(k)) for k in range(W - 14, 13, -2)]
    d.polygon([(x * S, y * S) for x, y in top + bottom], fill=BACON_FAT)
    save(img, "bacon")


def fries():
    """The carton of fries shown by an order that wants them on the side (UIOrderTicket)."""
    w, h = 96, 112
    img = Image.new("RGBA", (w * S, h * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    def stick(cx, top, tilt, colour, grow=0.0):
        # a fry from the carton's mouth (y 62) up to `top`, leaning `tilt` degrees
        a = math.radians(tilt)
        length = 62 - top
        half = 6.0 + grow
        ux, uy = math.sin(a), -math.cos(a)          # along the fry, upwards
        px, py = -uy, ux                            # across it
        base = (cx, 62.0)
        tip = (cx + ux * length, 62.0 + uy * length)
        ext = grow
        pts = [(base[0] + px * half, base[1] + py * half), (tip[0] + px * half - ux * -ext, tip[1] + py * half - uy * -ext),
               (tip[0] - px * half - ux * -ext, tip[1] - py * half - uy * -ext), (base[0] - px * half, base[1] - py * half)]
        d.polygon([(x * S, y * S) for x, y in pts], fill=colour)
    sticks = [(32, 20, -12), (60, 18, 10), (44, 8, -4), (54, 12, 4), (38, 24, -7), (66, 26, 14), (50, 22, 0)]
    for cx, top, tilt in sticks:
        stick(cx, top, tilt, INK, grow=3.5)
    for cx, top, tilt in sticks:
        stick(cx, top, tilt, FRY)
    for cx, top, tilt in sticks[2:4]:
        stick(cx, top + 30, tilt, FRY_DARK, grow=-3.0)
    # the carton: a red cup, wider at the top, with a light band under its lip
    outer = [(10, 52), (86, 52), (74, 108), (22, 108)]
    d.polygon([(x * S, y * S) for x, y in outer], fill=INK)
    i = LINE
    inner = [(10 + i + 1, 52 + i), (86 - i - 1, 52 + i), (74 - i + 1, 108 - i), (22 + i - 1, 108 - i)]
    d.polygon([(x * S, y * S) for x, y in inner], fill=CARTON)
    band = [(16 + i, 62), (80 - i, 62), (78 - i, 72), (18 + i, 72)]
    d.polygon([(x * S, y * S) for x, y in band], fill=CARTON_LIGHT)
    img = img.resize((w, h), Image.LANCZOS)
    img.save(os.path.join(OUT, "side_fries.png"))


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    every = {"bun_top": bun_top, "bun_bottom": bun_bottom, "burger": patty, "cheese": cheese, "lettuce": lettuce,
             "tomato": tomato, "onion": onion, "bacon": bacon, "fries": fries}
    for name in (sys.argv[1:] or every):
        every[name]()
    print("wrote layers to", os.path.abspath(OUT))
