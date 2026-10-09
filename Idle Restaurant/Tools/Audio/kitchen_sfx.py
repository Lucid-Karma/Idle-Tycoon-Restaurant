"""Synthesises the kitchen's sounds (none exist in the project's packs): knife on wood, a sizzle loop,
a 'ready' ding, a burnt 'poof', and a soft pick-up / put-down. 44.1 kHz mono 16-bit."""
import numpy as np, wave, os
from scipy import signal
SR = 44100
# Writes straight into the game: Assets/Project/[GAME]/Graphics/Audio/Kitchen (needs numpy + scipy).
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Assets", "Project", "[GAME]", "Graphics", "Audio", "Kitchen")
rng = np.random.default_rng(7)

def t(sec): return np.arange(int(SR * sec)) / SR
def env(n, attack, decay):  # attack seconds, exponential decay time-constant
    x = np.arange(n) / SR
    a = np.clip(x / max(attack, 1e-4), 0, 1)
    return a * np.exp(-np.maximum(x - attack, 0) / decay)
def band(x, lo, hi, order=2):
    b, a = signal.butter(order, [lo / (SR / 2), hi / (SR / 2)], 'band'); return signal.lfilter(b, a, x)
def lp(x, f, order=2):
    b, a = signal.butter(order, f / (SR / 2), 'low'); return signal.lfilter(b, a, x)
def hp(x, f, order=2):
    b, a = signal.butter(order, f / (SR / 2), 'high'); return signal.lfilter(b, a, x)
def save(name, x, peak=0.89):
    x = x / (np.max(np.abs(x)) + 1e-9) * peak
    fade = min(len(x), int(SR * 0.004)); x[-fade:] *= np.linspace(1, 0, fade)
    with wave.open(os.path.join(OUT, name), 'wb') as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes((x * 32767).astype(np.int16).tobytes())
    print(name, f"{len(x)/SR:.2f}s")

# knife on a board: the knife crunching through the vegetable, then the board's dull wooden knock. The board is
# noise ringing through a few damped resonators (wood is inharmonic and dead): pure decaying sines, the first
# try, sounded like a xylophone ("it doesn't sound like hitting a board"). Four takes, so repeats don't grate.
def resonate(x, f, q, gain):
    b, a = signal.iirpeak(f / (SR / 2), q)
    return signal.lfilter(b, a, x) * gain

def chop(seed):
    r = np.random.default_rng(seed)
    n = int(SR * 0.16); out = np.zeros(n)
    # the vegetable: a short crisp crunch of tiny clicks, ~25 ms before the blade reaches the board
    crunch_len = int(SR * r.uniform(0.018, 0.03))
    crunch = band(r.standard_normal(crunch_len), 1800, 7000, 2) * env(crunch_len, 0.002, 0.008)
    for _ in range(r.integers(4, 8)):
        p = r.integers(0, crunch_len - 60)
        crunch[p:p + 40] += hp(r.standard_normal(40), 2500) * env(40, 0.0001, 0.0006) * r.uniform(0.5, 1.2)
    hit = crunch_len + int(SR * 0.004)
    out[:crunch_len] += crunch * r.uniform(0.25, 0.4)
    # the board: a 3 ms burst of noise exciting its modes
    m = n - hit
    excite = r.standard_normal(m) * env(m, 0.0003, 0.0015)
    k = r.uniform(0.92, 1.08)
    board = (resonate(excite, 210 * k, 9, 1.0) + resonate(excite, 470 * k, 11, 0.8) +
             resonate(excite, 830 * k, 12, 0.5) + resonate(excite, 1450 * k, 10, 0.3) +
             resonate(excite, 2600 * k, 6, 0.15))
    board *= env(m, 0.0005, 0.03) * 2                              # wood is dead: no ringing tail
    thump = lp(r.standard_normal(m), 160) * env(m, 0.001, 0.02) * 3.5  # the counter under it
    tick = hp(r.standard_normal(m), 3000) * env(m, 0.0002, 0.0015) * 0.35  # the blade's edge
    out[hit:] += board + thump + tick
    return lp(out, 7000)

for i in range(4):
    save(f"kitchen_chop_{i + 1}.wav", chop(100 + i), 0.85)

# sizzle: bright hiss that breathes, with fat crackles; 3 s, seamless loop (crossfaded ends)
L = 3.0; n = int(SR * L); xf = int(SR * 0.4)
raw = rng.standard_normal(n + xf)
hiss = band(raw, 2200, 7000, 3)
breathe = 0.75 + 0.25 * lp(rng.standard_normal(n + xf), 3) * 8
hiss *= np.clip(breathe, 0.4, 1.1)
crackle = np.zeros(n + xf)
for _ in range(int(L * 38)):
    p = rng.integers(0, n + xf - 800); k = rng.integers(60, 600)
    crackle[p:p + k] += hp(rng.standard_normal(k), 1800) * env(k, 0.0002, 0.0025) * rng.uniform(0.6, 2.2)
s = hiss * 0.5 + crackle
loop = s[:n].copy(); ramp = np.linspace(0, 1, xf)
loop[:xf] = s[:xf] * ramp + s[n:n + xf] * (1 - ramp)   # the tail fades into the head
loop = loop / (np.max(np.abs(loop)) + 1e-9) * 0.8
with wave.open(os.path.join(OUT, "kitchen_sizzle_loop.wav"), 'wb') as w:
    w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR); w.writeframes((loop * 32767).astype(np.int16).tobytes())
print("kitchen_sizzle_loop.wav", L)

# ready: a small bright two-note bell (a kitchen timer's 'ding-ding' would nag; one rising pair is friendly)
def bell(f, dur, decay):
    x = t(dur); n = len(x)
    partials = [(1, 1.0, 1), (2.0, 0.35, 0.7), (2.76, 0.22, 0.45), (5.4, 0.08, 0.25)]
    out = sum(a * np.sin(2*np.pi*f*m*x) * env(n, 0.002, decay * d) for m, a, d in partials)
    return out * np.linspace(1, 0, n) ** 0.5   # rings out to silence: no click where it ends
a = bell(1318.5, 0.9, 0.28); b = bell(1760.0, 0.9, 0.35)
out = np.zeros(int(SR * 1.0)); out[:len(a)] += a * 0.8; off = int(SR * 0.09); out[off:off + len(b)] += b[:len(out) - off]
save("kitchen_ready.wav", out, 0.8)

# burnt: a soft "fsss" of smoke that swells in and dies away - no sudden puff (the first one, a 15 ms attack
# with crackles, made the user jump). Low and breathy, a little brighter hiss on top, all smooth.
n = int(SR * 0.95); x = np.arange(n) / SR
swell = np.sin(np.clip(x / 0.18, 0, 1) * np.pi / 2) ** 2 * np.exp(-np.maximum(x - 0.18, 0) / 0.28)
breath = lp(rng.standard_normal(n), 1400, 3)
hiss = band(rng.standard_normal(n), 2500, 6000, 2) * 0.25
save("kitchen_burnt.wav", (breath + hiss) * swell, 0.5)

# pick up: a short soft rising 'bloop'; put down: a lower soft 'tup'
def bloop(f0, f1, dur, decay):
    x = t(dur); f = np.linspace(f0, f1, len(x)); ph = 2*np.pi*np.cumsum(f) / SR
    return np.sin(ph) * env(len(x), 0.003, decay)
save("kitchen_pickup.wav", bloop(420, 760, 0.12, 0.035), 0.7)
d = bloop(330, 190, 0.12, 0.03) + lp(rng.standard_normal(int(SR*0.12)), 1500) * env(int(SR*0.12), 0.001, 0.012) * 0.25
save("kitchen_putdown.wav", d, 0.7)
