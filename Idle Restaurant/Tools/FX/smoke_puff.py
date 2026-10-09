"""The soft smoke puff for BurnSmoke: a few overlapping round blobs, white, the shape in the alpha (128 px).
Writes Assets/Project/[GAME]/Graphics/Sprites/FX/smoke_puff.png. Needs numpy + Pillow."""
import os
import numpy as np
from PIL import Image

N = 128; y, x = np.mgrid[0:N, 0:N] / (N - 1) * 2 - 1
a = np.zeros((N, N))
for cx, cy, r in [(0, 0, 0.55), (-0.3, -0.12, 0.38), (0.3, -0.1, 0.4), (-0.1, 0.25, 0.36), (0.22, 0.22, 0.32)]:
    a += np.exp(-((x - cx) ** 2 + (y - cy) ** 2) / (2 * (r * 0.6) ** 2))
a = a / a.max()
edge = np.clip(1 - np.sqrt(x ** 2 + y ** 2), 0, 1)          # always fades out before the square's edge
a = np.clip(a * 1.25, 0, 1) * np.clip(edge * 2.5, 0, 1)
rgb = np.clip(0.82 + 0.18 * np.clip(1 - (y + 0.4), 0, 1), 0, 1)   # a touch lighter on top
out = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Assets", "Project", "[GAME]", "Graphics", "Sprites", "FX", "smoke_puff.png")
Image.fromarray((np.dstack([rgb, rgb, rgb, a]) * 255).astype(np.uint8), 'RGBA').save(out)
