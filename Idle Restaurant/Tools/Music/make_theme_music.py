# Makes the cafe's music loop's themed versions (Halloween, Winter) as seamless 16-bit WAV loops (Valentine is a
# re-arrangement of its own: make_valentine_music.py):
# the music's pitch is never touched (the game's own sounds stay in tune with it); it is coloured (EQ, reverb, a
# little tape warble or chorus) and layered with sounds placed exactly on the loop's own beat grid.
# Every process wraps round the loop's end (circular reverb, filters run in steady state, LFOs whose periods divide
# the loop), so the loop stays seamless.
import sys
import numpy as np
from scipy.io import wavfile
from scipy.signal import sosfilt, butter
import warnings
warnings.filterwarnings('ignore')

SRC = r'C:\Users\Beyzanur\Desktop\Projects\UnityProjectsIDo\SERIOUS\Idle Restaurant\Idle-Tycoon-Restaurant\Idle Restaurant\Assets\Audio\A-Symphony-of-Disarray_GameMusicPack\01 Loops\0110_A-Symphony-of-Disarray_Loop-1_114bpm4-4_L24M.wav'
OUT = sys.argv[1] if len(sys.argv) > 1 else '.'
sr, raw = wavfile.read(SRC)
music = raw.astype(np.float64) / 2**31          # (N, 2)
N = len(music)
BEAT = 60.0 / 114.0
BAR = 4 * BEAT
LOOP = N / sr                                    # 24 bars
rng = np.random.default_rng(7)

# The first downbeat: the strongest low-end rise in the first 80 ms.
low = sosfilt(butter(2, 150, 'low', fs=sr, output='sos'), music.mean(axis=1))
env = np.abs(low[: int(0.08 * sr)])
D0 = 0.0 if env.max() < 1e-4 else max(0.0, (np.argmax(env > env.max() * 0.5)) / sr - 0.002)
print('downbeat at', round(D0, 4), 's; loop', round(LOOP, 4), 's =', round(LOOP / BAR, 3), 'bars')


def beats():
    t = D0
    while t < LOOP - 1e-6:
        yield t
        t += BEAT


# ---- tools ----------------------------------------------------------------------------------------

def steady(sos, x):
    # an IIR filter in steady state over a loop: run it over the loop twice, keep the second pass
    y = sosfilt(sos, np.concatenate([x, x], axis=0), axis=0)
    return y[len(x):]


def biquad(kind, f0, gain_db=0.0, q=0.707):
    A = 10 ** (gain_db / 40)
    w = 2 * np.pi * f0 / sr
    cw, sw = np.cos(w), np.sin(w)
    alpha = sw / (2 * q)
    if kind == 'lowshelf':
        b0 = A * ((A + 1) - (A - 1) * cw + 2 * np.sqrt(A) * alpha)
        b1 = 2 * A * ((A - 1) - (A + 1) * cw)
        b2 = A * ((A + 1) - (A - 1) * cw - 2 * np.sqrt(A) * alpha)
        a0 = (A + 1) + (A - 1) * cw + 2 * np.sqrt(A) * alpha
        a1 = -2 * ((A - 1) + (A + 1) * cw)
        a2 = (A + 1) + (A - 1) * cw - 2 * np.sqrt(A) * alpha
    elif kind == 'highshelf':
        b0 = A * ((A + 1) + (A - 1) * cw + 2 * np.sqrt(A) * alpha)
        b1 = -2 * A * ((A - 1) + (A + 1) * cw)
        b2 = A * ((A + 1) + (A - 1) * cw - 2 * np.sqrt(A) * alpha)
        a0 = (A + 1) - (A - 1) * cw + 2 * np.sqrt(A) * alpha
        a1 = 2 * ((A - 1) - (A + 1) * cw)
        a2 = (A + 1) - (A - 1) * cw - 2 * np.sqrt(A) * alpha
    else:
        raise ValueError(kind)
    return np.array([[b0 / a0, b1 / a0, b2 / a0, 1.0, a1 / a0, a2 / a0]])


def reverb(x, seconds, predelay=0.02, damp=4000, width=1.0):
    # a smooth, decorrelated stereo tail (decaying noise, darker as it fades), applied circularly
    n = int(seconds * sr)
    t = np.arange(n) / sr
    ir = np.zeros((N, 2))
    for c in range(2):
        noise = rng.standard_normal(n)
        noise = sosfilt(butter(1, damp, 'low', fs=sr, output='sos'), noise)
        tail = noise * np.exp(-6.9 * t / seconds)
        p = int(predelay * sr) + c * int(0.007 * sr * width)
        ir[p:p + n, c] = tail[: N - p]
    ir /= np.sqrt((ir ** 2).sum(axis=0))
    X = np.fft.rfft(x, axis=0)
    H = np.fft.rfft(ir, axis=0)
    return np.fft.irfft(X * H, n=N, axis=0)


def modulated_delay(x, base_ms, depth_ms, period, phase=0.0):
    # a delay that swings sinusoidally (period divides the loop), read circularly with linear interpolation
    n = np.arange(N)
    d = (base_ms + depth_ms * np.sin(2 * np.pi * n / (period * sr) + phase)) * sr / 1000
    pos = (n - d) % N
    i0 = np.floor(pos).astype(int)
    frac = pos - i0
    i1 = (i0 + 1) % N
    return x[i0] * (1 - frac)[:, None] + x[i1] * frac[:, None] if x.ndim == 2 else x[i0] * (1 - frac) + x[i1] * frac


def place(track, at, sound, pan=0.0, gain=1.0):
    # adds a (mono or stereo) sound at `at` seconds, wrapping past the loop's end
    s = sound if sound.ndim == 2 else np.stack([sound, sound], axis=1)
    s = s * gain * np.array([np.sqrt(0.5 * (1 - pan)) * 1.414, np.sqrt(0.5 * (1 + pan)) * 1.414])
    i = int(round(at * sr)) % N
    k = len(s)
    first = min(k, N - i)
    track[i:i + first] += s[:first]
    if k > first:
        track[: k - first] += s[first:]


def rms(x):
    return float(np.sqrt((x ** 2).mean()))


def finish(name, x):
    # same loudness as the original, peaks held under full scale by a soft knee
    x = x * (rms(music) / rms(x))
    knee = 0.85
    over = np.abs(x) > knee
    x[over] = np.sign(x[over]) * (knee + (1 - knee) * np.tanh((np.abs(x[over]) - knee) / (1 - knee)))
    path = f'{OUT}/music_loop_{name}.wav'
    wavfile.write(path, sr, (np.clip(x, -1, 1) * 32767).astype(np.int16))
    print(name, 'rms', round(rms(x), 4), 'peak', round(float(np.abs(x).max()), 3), '->', path)


# ---- sounds -----------------------------------------------------------------------------------------

def jingle_hit(strength, spread=0.025, count=9):
    # a hand of sleigh bells shaken once: several little bells, each a few inharmonic partials ringing briefly,
    # struck within a few milliseconds of each other, over a short metallic rattle
    length = int(0.35 * sr)
    out = np.zeros((length, 2))
    for _ in range(count):
        f = rng.uniform(3200, 5600)
        decay = rng.uniform(0.045, 0.11)
        start = int(rng.uniform(0, spread) * sr)
        t = np.arange(length - start) / sr
        bell = sum(a * np.sin(2 * np.pi * f * r * t + rng.uniform(0, 6.28)) for r, a in ((1, 1), (1.47, 0.55), (2.09, 0.3), (2.56, 0.18)))
        bell *= np.exp(-t / decay) * np.minimum(1, t / 0.0015)
        pan = rng.uniform(-0.5, 0.5)
        out[start:, 0] += bell * (1 - pan) * 0.5
        out[start:, 1] += bell * (1 + pan) * 0.5
    t = np.arange(length) / sr
    rattle = sosfilt(butter(2, [4000, 8000], 'band', fs=sr, output='sos'), rng.standard_normal(length)) * np.exp(-t / 0.025)
    out += rattle[:, None] * 0.3
    return out / np.abs(out).max() * strength


def jingle_roll(seconds, strength):
    # a long shake: hits close together, swelling, landing on the next downbeat
    length = int(seconds * sr)
    out = np.zeros((length + int(0.4 * sr), 2))
    t = 0.0
    while t < seconds:
        k = int(t * sr)
        swell = 0.35 + 0.65 * (t / seconds) ** 1.5
        h = jingle_hit(strength * swell, spread=0.012, count=5)
        out[k:k + len(h)] += h
        t += rng.uniform(0.035, 0.05)
    return out


def woo(length=1.9):
    # one long ghostly "wooooo": a breathy "oo" that swells, rises a little and sinks away, with a slow wobble
    t = np.arange(int(length * sr)) / sr
    u = t / length
    f = 300 * (1 + 0.28 * np.sin(np.pi * np.minimum(1, u * 1.4)) - 0.18 * u ** 2) * (1 + 0.012 * np.sin(2 * np.pi * 4.5 * t))
    ph = 2 * np.pi * np.cumsum(f) / sr
    env = np.sin(np.pi * np.minimum(1, u * 2.2)) ** 2 * np.where(u < 0.45, 1, np.cos(np.pi * (u - 0.45) / 1.1) ** 2)
    tone = np.sin(ph) + 0.18 * np.sin(2 * ph) + 0.05 * np.sin(3 * ph)
    breath = sosfilt(butter(2, [250, 1100], 'band', fs=sr, output='sos'), rng.standard_normal(len(t))) * 0.22
    return sosfilt(butter(2, 900, 'low', fs=sr, output='sos'), tone + breath) * env


def thunder(seconds=3.2):
    n = int(seconds * sr)
    t = np.arange(n) / sr
    noise = rng.standard_normal((n, 2))
    rumble = sosfilt(butter(4, 140, 'low', fs=sr, output='sos'), noise, axis=0)
    crack = sosfilt(butter(2, [300, 1500], 'band', fs=sr, output='sos'), noise, axis=0) * np.exp(-t / 0.25)[:, None] * 0.35
    env = np.minimum(1, t / 0.15) * np.exp(-t / 1.1) * (1 + 0.6 * np.sin(2 * np.pi * 1.7 * t) * np.exp(-t / 1.5))
    return (rumble * 3 + crack) * env[:, None]


def wind():
    # breathy noise in a slowly moving band, swelling twice a loop
    noise = rng.standard_normal((N, 2))
    lo = steady(butter(2, [220, 900], 'band', fs=sr, output='sos'), noise)
    hi = steady(butter(2, [700, 2200], 'band', fs=sr, output='sos'), noise)
    t = np.arange(N) / sr
    drift = 0.85 + 0.15 * np.cos(2 * np.pi * t / (LOOP / 2))
    return lo * drift[:, None] + hi * 0.25


# ---- the themes -------------------------------------------------------------------------------------

def halloween():
    x = steady(biquad('highshelf', 3500, -5.0), music)                    # darker
    x = steady(biquad('lowshelf', 180, 1.5), x)
    x = modulated_delay(x, 4.0, 1.4, BAR * 2)                              # an old tape's gentle warble (< 10 cents)
    x = 0.78 * x + 0.32 * reverb(x, 3.2, predelay=0.03, damp=3500)         # a big, spooky room
    # a faint, steady breeze (a swelling wind read as sea waves - user, 2026-10-27)
    w = wind(); amb = 0.1 * w / rms(w) * rms(music)
    for bar in (2, 14):
        b = D0 + bar * BAR
        place(amb, b, woo(), pan=-0.35, gain=0.55)                       # a single ghost's "woooo" (it was a three-note hoot)
    th = thunder()
    place(amb, D0 + 8 * BAR - 0.1, th / np.abs(th).max(), pan=0.2, gain=0.45)
    amb = amb + 0.4 * reverb(amb, 2.5, damp=3000)
    return x + amb * 0.55


def winter():
    x = steady(biquad('highshelf', 6000, 2.0), music)                      # a little sparkle
    x = 0.85 * x + 0.22 * reverb(x, 2.4, predelay=0.025, damp=7000)        # a warm hall
    bells = np.zeros((N, 2))
    for i, b in enumerate(beats()):
        bar_beat = i % 4
        bar = i // 4
        if bar % 4 == 3 and bar_beat == 3:
            continue                                                       # the roll takes this beat
        place(bells, b, jingle_hit(1.0 if bar_beat in (1, 3) else 0.6))     # on every beat, leaning on 2 and 4
    for bar in range(7, 24, 8):                                            # a shake into every eighth downbeat
        roll = jingle_roll(BEAT * 0.95, 0.9)
        place(bells, D0 + bar * BAR + 3 * BEAT, roll)
    bells = steady(biquad('lowshelf', 2500, -6.0), bells)
    bells = steady(biquad('highshelf', 6500, -6.0), bells)              # rounder, never shrill
    bells = 0.6 * bells + 0.5 * reverb(bells, 1.8, damp=7000)           # a little way off, in the same room as the music
    bells *= 0.075 * rms(music) / rms(bells)                            # under the music (they drowned it - user)
    return x + bells


themes = sys.argv[2:] or ['halloween', 'winter']                       # e.g. "make_theme_music.py <out> halloween"
if 'halloween' in themes:
    finish('halloween', halloween())
if 'winter' in themes:
    finish('winter', winter())
