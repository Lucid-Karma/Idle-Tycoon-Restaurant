# icon_money.png: a banknote in the UI icon family (white silhouette on transparent, 128 px, tinted in Unity).
# Used for "Earned today" on the break card (UIBreakCard). Run with a Python that has Pillow.
from PIL import Image, ImageDraw, ImageFont
import os

S = 4                       # supersampling
N = 128 * S
here = os.path.dirname(os.path.abspath(__file__))
out = os.path.join(here, "..", "..", "Assets", "Project", "[GAME]", "Graphics", "Sprites", "UI", "icon_money.png")
font = os.path.join(here, "..", "..", "Assets", "Project", "[GAME]", "Graphics", "Font", "Fredoka", "Fredoka-SemiBold.ttf")

mask = Image.new("L", (N, N), 0)
d = ImageDraw.Draw(mask)
x0, y0, x1, y1 = 8 * S, 28 * S, 120 * S, 100 * S
d.rounded_rectangle((x0, y0, x1, y1), radius=12 * S, fill=255)
# an inner border line, cut out
d.rounded_rectangle((x0 + 9 * S, y0 + 9 * S, x1 - 9 * S, y1 - 9 * S), radius=6 * S, outline=0, width=4 * S)
# the medallion: a cut-out circle with a solid "$" in it
cx, cy, r = N // 2, (y0 + y1) // 2, 22 * S
d.ellipse((cx - r, cy - r, cx + r, cy + r), fill=0)
f = ImageFont.truetype(font, 40 * S)
d.text((cx, cy + 2 * S), "$", font=f, fill=255, anchor="mm")
# corner dots
for px in (x0 + 24 * S, x1 - 24 * S):
    d.ellipse((px - 5 * S, cy - 5 * S, px + 5 * S, cy + 5 * S), fill=0)

mask = mask.resize((128, 128), Image.LANCZOS)
img = Image.new("RGBA", (128, 128), (255, 255, 255, 0))
img.putalpha(mask)
img.save(out)
print("wrote", os.path.normpath(out))
