# Valentine's music: a calm, original little piece instead of a re-dressed cafe loop (user, 2026-10-27: the
# re-orchestration sounded "chaotic, like a church organ, scary"; wanted calm and soothing, at most two instruments,
# the melody may change). What makes music calm, and what this follows: a slow tempo (60-75 bpm, near a resting
# pulse), a gentle 3/4 sway (Satie's Gymnopedies), a major key with soft diatonic chords (maj7 / m7 colours, no
# chromatic clashes), a simple singable melody of long notes that moves by step, a sparse texture (one instrument
# carrying it, a second only adding warmth), narrow soft dynamics, no percussion, a sustain pedal and a warm room.
#   felt piano: a slow rocking left hand (root, then the chord) and the melody - the whole piece
#   cello: long, soft bass notes under the second and third phrases only
# C major, 70 bpm, 24 bars of 3/4 (three 8-bar phrases A B A'), ~61.7 s, a seamless loop (tails and the room wrap
# round its end). Tuned to A440 like the game's other sounds. usage: python make_valentine_music.py <out dir>
import sys
import numpy as np
from scipy.io import wavfile
from scipy.signal import sosfilt, butter

OUT = sys.argv[1] if len(sys.argv) > 1 else '.'
sr = 48000
BPM = 70.0
BEAT = 60.0 / BPM
BAR = 3 * BEAT
BARS = 24
LOOP = BARS * BAR
N = int(round(LOOP * sr))
rng = np.random.default_rng(3)
mf = lambda m: 440.0 * 2 ** ((m - 69) / 12)

# ---- the score ------------------------------------------------------------------------------------
C, D, E, F, G, A, B = 0, 2, 4, 5, 7, 9, 11
# one chord a bar: (bass note, chord tones above it)
CHORDS = {
    'Cmaj7': (36, [48, 52, 55, 59]), 'Am7': (45, [48, 52, 55, 57]), 'Fmaj7': (41, [48, 52, 53, 57]),
    'G': (43, [47, 50, 55, 59]), 'Gsus': (43, [48, 50, 55, 57]), 'Em7': (40, [47, 50, 52, 55]),
    'C/E': (40, [48, 52, 55, 60]), 'Dm7': (38, [48, 50, 53, 57]),
}
PROGRESSION = ['Cmaj7', 'Am7', 'Fmaj7', 'G', 'Cmaj7', 'Em7', 'Fmaj7', 'Gsus',
               'Am7', 'Em7', 'Fmaj7', 'C/E', 'Dm7', 'G', 'Cmaj7', 'Gsus',
               'Cmaj7', 'Am7', 'Fmaj7', 'G', 'Cmaj7', 'Em7', 'Fmaj7', 'G']
# the melody: (bar, beat, beats long, midi)
MELODY = [
    (0, 0, 2, 76), (0, 2, 1, 74), (1, 0, 3, 72), (2, 0, 1, 69), (2, 1, 1, 72), (2, 2, 1, 76), (3, 0, 3, 74),
    (4, 0, 1.5, 76), (4, 1.5, 1.5, 79), (5, 0, 2, 76), (5, 2, 1, 74), (6, 0, 1, 72), (6, 1, 2, 69), (7, 0, 3, 74),
    (8, 0, 2, 72), (8, 2, 1, 76), (9, 0, 2, 79), (9, 2, 1, 76), (10, 0, 1, 77), (10, 1, 1, 76), (10, 2, 1, 72),
    (11, 0, 3, 76), (12, 0, 2, 77), (12, 2, 1, 76), (13, 0, 2, 74), (13, 2, 1, 71), (14, 0, 1, 72), (14, 1, 2, 76),
    (15, 0, 3, 74),
    (16, 0, 2, 79), (16, 2, 1, 76), (17, 0, 1.5, 72), (17, 1.5, 1.5, 76), (18, 0, 1, 69), (18, 1, 1, 72),
    (18, 2, 1, 77), (19, 0, 1, 76), (19, 1, 2, 74), (20, 0, 2, 76), (20, 2, 1, 79), (21, 0, 1, 79), (21, 1, 2, 76),
    (22, 0, 1, 74), (22, 1, 1, 72), (22, 2, 1, 69), (23, 0, 1.5, 71), (23, 1.5, 1.5, 74),
]


# ---- instruments ----------------------------------------------------------------------------------

def piano(freq, velocity, hold):
    # a felt piano note: two slightly detuned strings per note (a slow beat), slightly stretched partials,
    # high partials dying fast, a soft felt attack and a quick damper release after `hold` seconds
    length = hold + 1.2
    n = int(length * sr)
    t = np.arange(n) / sr
    base_tau = 4.5 * (261.6 / freq) ** 0.45
    bright = 900 + 2200 * velocity
    out = np.zeros(n)
    for k in range(1, 13):
        fk = freq * k * np.sqrt(1 + 0.00035 * k * k)
        if fk > 7000:
            break
        amp = (1.0 / k ** 1.1) * np.exp(-fk / bright)
        tau = base_tau / (1 + 0.55 * (k - 1))
        decay = 0.65 * np.exp(-t / (tau * 0.25)) + 0.35 * np.exp(-t / tau)   # the quick fall, then the long ring
        for cents, ph in ((-0.9, 0.0), (0.9, 1.3)):
            out += 0.5 * amp * decay * np.sin(2 * np.pi * fk * 2 ** (cents / 1200) * t + ph * k)
    out *= np.minimum(1, t / 0.006)
    damper = np.where(t < hold, 1.0, np.exp(-(t - hold) / 0.22))
    out *= damper
    thump = sosfilt(butter(2, [80, 600], 'band', fs=sr, output='sos'), rng.standard_normal(n)) * np.exp(-t / 0.012)
    return (out + thump * 0.02) * velocity


def cello(freq, seconds, strength):
    # a soft bowed cello: a band-limited saw with a late, gentle vibrato, through the body's resonances (so the
    # vibrato moves its colour - a steady, plain harmonic tone is what sounds like an organ), slow bow-in/out,
    # a breath of bow noise
    n = int(seconds * sr)
    t = np.arange(n) / sr
    vib = 1 + 0.0028 * np.sin(2 * np.pi * 5.0 * t + rng.uniform(0, 6)) * np.clip((t - 0.45) / 0.6, 0, 1)
    pressure = 1 + 0.08 * sosfilt(butter(1, 3, 'low', fs=sr, output='sos'), rng.standard_normal(n)) * 30
    ph = 2 * np.pi * np.cumsum(freq * vib) / sr
    tone = np.zeros(n)
    for k in range(1, 30):
        if freq * k > 5000:
            break
        tone += np.sin(k * ph) / k
    body = (sosfilt(butter(2, [180, 320], 'band', fs=sr, output='sos'), tone) * 1.0
            + sosfilt(butter(2, [420, 700], 'band', fs=sr, output='sos'), tone) * 0.7
            + sosfilt(butter(2, [1000, 1600], 'band', fs=sr, output='sos'), tone) * 0.25
            + tone * 0.15)
    body = sosfilt(butter(2, 1800, 'low', fs=sr, output='sos'), body)
    bow = sosfilt(butter(2, [1200, 3500], 'band', fs=sr, output='sos'), rng.standard_normal(n)) * 0.01
    env = np.clip(t / 0.7, 0, 1) ** 1.5 * np.clip((seconds - t) / 0.9, 0, 1)
    return (body * pressure + bow) * env * strength


def place(track, at, sound, pan=0.0):
    s = np.stack([sound * np.sqrt(0.5 * (1 - pan)), sound * np.sqrt(0.5 * (1 + pan))], axis=1)
    i = int(round(at * sr)) % N
    first = min(len(s), N - i)
    track[i:i + first] += s[:first]
    if len(s) > first:
        track[: len(s) - first] += s[first:]


def room(x, seconds=2.6, damp=3500):
    # a warm, circular room: decaying, darkening noise per channel
    n = int(seconds * sr)
    t = np.arange(n) / sr
    ir = np.zeros((N, 2))
    for c in range(2):
        noise = sosfilt(butter(1, damp, 'low', fs=sr, output='sos'), rng.standard_normal(n))
        p = int(0.02 * sr) + c * int(0.009 * sr)
        ir[p:p + n, c] = (noise * np.exp(-6.9 * t / seconds))[: N - p]
    ir /= np.sqrt((ir ** 2).sum(axis=0))
    return np.fft.irfft(np.fft.rfft(x, axis=0) * np.fft.rfft(ir, axis=0), n=N, axis=0)


def rms(x):
    return float(np.sqrt((x ** 2).mean()))


# ---- the performance ----------------------------------------------------------------------------------
keys = np.zeros((N, 2))
cellos = np.zeros((N, 2))
for bar, name in enumerate(PROGRESSION):
    bass, tones = CHORDS[name]
    t0 = bar * BAR
    sway = 0.012 * np.sin(2 * np.pi * bar / 8)                         # a breath of rubato, back in time by the bar
    # left hand: the bass on one, the chord softly on two (held through three) - the rocking of a Gymnopedie
    place(keys, t0 + sway, piano(mf(bass), 0.42, BAR - 0.05), pan=-0.25)
    voiced = sorted(m + 12 if m < 52 else m for m in tones)            # (lifted clear of the bass: no mud)
    chord = sum(piano(mf(m), 0.2 + 0.03 * i, 2 * BEAT - 0.05) for i, m in enumerate(voiced))
    place(keys, t0 + BEAT + sway + 0.015, chord, pan=-0.05)
    if 8 <= bar:                                                       # the cello joins for phrases two and three
        place(cellos, t0 - 0.12, cello(mf(bass + 12 if bass < 40 else bass), BAR + 0.5, 1.0), pan=-0.15)
for bar, beat, beats, m in MELODY:
    at = bar * BAR + beat * BEAT
    accent = 0.62 if beat == 0 else 0.5
    place(keys, at + 0.01, piano(mf(m), accent, beats * BEAT + 0.15), pan=0.15)

keys = sosfilt(butter(2, 4200, 'low', fs=sr, output='sos'), np.concatenate([keys, keys]), axis=0)[N:]   # felt: soft
cellos *= 0.38 * rms(keys) / max(rms(cellos), 1e-9)                    # under the piano: warmth, not a second lead
mix = keys + cellos
mix = 0.78 * mix + 0.3 * room(mix)

# Quiet: about half the cafe loop's loudness (the user: too loud), peaks well under full scale.
mix *= 0.075 / rms(mix)
peak = float(np.abs(mix).max())
if peak > 0.6:
    mix *= 0.6 / peak
path = f'{OUT}/music_loop_valentine.wav'
wavfile.write(path, sr, (mix * 32767).astype(np.int16))
print('valentine', round(LOOP, 2), 's rms', round(rms(mix), 4), 'peak', round(float(np.abs(mix).max()), 3), '->', path)
