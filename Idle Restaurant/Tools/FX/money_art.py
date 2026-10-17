"""The money piles' art (MoneyPiles): writes to Assets/Project/[GAME]/Graphics/Sprites/FX/
  bill_face.png  256 x 128, the face of a banknote: a soft mint green (cozy, not dollar-grey), a darker border,
                 corner rosettes and a "$" in an oval in the middle, in the game's own font (Fredoka SemiBold).
  sparkle.png     64 px, a white four-pointed glint (shape in the alpha) that flashes on a waiting pile.
Needs numpy + Pillow."""
import os
import numpy as np
from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
GAME = os.path.join(HERE, "..", "..", "Assets", "Project", "[GAME]")
OUT = os.path.join(GAME, "Graphics", "Sprites", "FX")
FONT = os.path.join(GAME, "Graphics", "Font", "Fredoka", "Fredoka-SemiBold.ttf")

PAPER = (170, 226, 178)    # the note: a soft mint, in the pastel of the cafe (a deep green was tried, and dropped)
LIGHT = (214, 245, 214)    # the oval behind the "$"
INK = (44, 120, 86)        # border, rosettes, "$"


def bill(w=256, h=128, s=4):
    W, H = w * s, h * s
    img = Image.new("RGB", (W, H), PAPER)
    d = ImageDraw.Draw(img)
    m = int(H * 0.07)
    d.rounded_rectangle((m, m, W - m, H - m), radius=int(H * 0.08), outline=INK, width=int(H * 0.035))
    m2 = int(H * 0.15)
    d.rounded_rectangle((m2, m2, W - m2, H - m2), radius=int(H * 0.05), outline=INK, width=int(H * 0.012))
    # Corner rosettes.
    r = int(H * 0.09)
    for cx in (int(W * 0.13), int(W * 0.87)):
        for cy in (int(H * 0.30), int(H * 0.70)):
            d.ellipse((cx - r, cy - r, cx + r, cy + r), outline=INK, width=int(H * 0.018))
            d.ellipse((cx - r // 3, cy - r // 3, cx + r // 3, cy + r // 3), fill=INK)
    # The oval and the "$".
    ow, oh = int(H * 0.42), int(H * 0.36)
    d.ellipse((W // 2 - ow, H // 2 - oh, W // 2 + ow, H // 2 + oh), fill=LIGHT, outline=INK, width=int(H * 0.02))
    font = ImageFont.truetype(FONT, int(H * 0.62))
    box = d.textbbox((0, 0), "$", font=font)
    d.text((W / 2 - (box[0] + box[2]) / 2, H / 2 - (box[1] + box[3]) / 2), "$", font=font, fill=INK)
    return img.resize((w, h), Image.LANCZOS)


def sparkle(n=64):
    y, x = np.mgrid[0:n, 0:n] / (n - 1) * 2 - 1
    ax, ay = np.abs(x), np.abs(y)
    # Two thin crossed spikes plus a bright round core.
    spikes = np.exp(-(ax / 0.07) ** 2) * np.clip(1 - ay, 0, 1) ** 1.2 + np.exp(-(ay / 0.07) ** 2) * np.clip(1 - ax, 0, 1) ** 1.2
    core = np.clip(1.6 * np.exp(-(x ** 2 + y ** 2) / (2 * 0.13 ** 2)), 0, 1)
    a = np.clip(spikes + core, 0, 1)
    rgb = np.ones((n, n, 3))
    return Image.fromarray((np.dstack([rgb, a]) * 255).astype(np.uint8), "RGBA")


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    bill().save(os.path.join(OUT, "bill_face.png"))
    sparkle().save(os.path.join(OUT, "sparkle.png"))
    print("wrote bill_face.png, sparkle.png to", OUT)
