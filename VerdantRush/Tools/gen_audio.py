"""Synthesised chiptune SFX and a looping zone theme (16-bit mono WAV)."""
import os, wave
import numpy as np

OUT = os.path.join(os.path.dirname(__file__), '..', 'Assets', 'Resources', 'Audio')
os.makedirs(OUT, exist_ok=True)
SR = 22050


def write(name, x, vol=0.6):
    x = np.clip(x * vol, -1, 1)
    with wave.open(os.path.join(OUT, name + '.wav'), 'wb') as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes((x * 32767).astype('<i2').tobytes())


def t(d):
    return np.arange(int(SR * d)) / SR


def sq(f, d, duty=0.5):
    ph = np.cumsum(np.broadcast_to(f, t(d).shape) / SR) % 1.0
    return np.where(ph < duty, 1.0, -1.0)


def tri(f, d):
    ph = np.cumsum(np.broadcast_to(f, t(d).shape) / SR) % 1.0
    return 4 * np.abs(ph - 0.5) - 1


def env(n, a=0.005, r=0.1, total=None):
    tt = np.arange(n) / SR
    total = total or n / SR
    e = np.minimum(1, tt / max(a, 1e-4)) * np.clip((total - tt) / max(r, 1e-4), 0, 1)
    return e


def noise(d):
    return np.random.RandomState(1).uniform(-1, 1, int(SR * d))


def sfx():
    d = 0.35
    x = np.concatenate([sq(1318, 0.07, 0.25), sq(1760, 0.28, 0.25)])
    write('ring', x * env(len(x), r=0.25), 0.35)
    d = 0.22; f = np.linspace(300, 900, int(SR * d))
    write('jump', sq(f, d, 0.25) * env(int(SR * d), r=0.1), 0.3)
    d = 0.35; f = np.linspace(200, 1400, int(SR * d)) + 60 * np.sin(t(d) * 80)
    write('spring', sq(f, d, 0.5) * env(int(SR * d), r=0.2), 0.3)
    d = 0.18; f = np.linspace(700, 1600, int(SR * d))
    write('dash', sq(f, d, 0.125) * env(int(SR * d), r=0.1), 0.3)
    d = 0.3; f = np.linspace(400, 1200, int(SR * d))
    write('rev', (sq(f, d, 0.25) * 0.6 + noise(d) * 0.3) * env(int(SR * d), r=0.2), 0.3)
    d = 0.45; f = np.linspace(1200, 200, int(SR * d))
    write('release', (sq(f, d, 0.5) * 0.5 + noise(d) * 0.4) * env(int(SR * d), r=0.35), 0.3)
    d = 0.6
    n = noise(d) * np.exp(-t(d) * 7)
    b = sq(np.linspace(180, 40, int(SR * d)), d) * np.exp(-t(d) * 6)
    write('boom', n * 0.7 + b * 0.5, 0.45)
    d = 0.9
    x = np.zeros(int(SR * d))
    rs = np.random.RandomState(4)
    for k in range(10):
        s = int(k * SR * 0.07)
        seg = sq(1500 + rs.randint(-300, 300), 0.1, 0.25) * env(int(SR * 0.1), r=0.08)
        x[s:s + len(seg)] += seg * (1 - k / 12)
    write('lose', x, 0.3)
    d = 0.5; f = np.linspace(600, 120, int(SR * d))
    write('hurt', sq(f, d, 0.5) * env(int(SR * d), r=0.3), 0.3)
    d = 0.25; f = np.linspace(500, 700, int(SR * d))
    write('skid', (noise(d) * 0.6 + sq(f, d, 0.1) * 0.2) * env(int(SR * d), r=0.1), 0.25)
    notes = [523, 659, 784, 1046]
    x = np.concatenate([sq(n, 0.09, 0.25) * env(int(SR * 0.09), r=0.05) for n in notes] + [sq(1318, 0.4, 0.25) * env(int(SR * 0.4), r=0.35)])
    write('check', x, 0.3)
    d = 0.25; f = np.linspace(900, 1800, int(SR * d))
    write('trick', sq(f, d, 0.25) * env(int(SR * d), r=0.15), 0.28)
    d = 0.6; f = 300 + 200 * np.sin(t(d) * 60)
    write('boost', (sq(f + np.linspace(0, 800, int(SR * d)), d, 0.5) * 0.4 + noise(d) * 0.4) * env(int(SR * d), r=0.4), 0.3)
    d = 0.15
    write('select', sq(880, d, 0.25) * env(int(SR * d), r=0.1), 0.3)


NOTE = {n: i for i, n in enumerate(['C', 'C#', 'D', 'D#', 'E', 'F', 'F#', 'G', 'G#', 'A', 'A#', 'B'])}


def freq(s):
    if s in ('-', '.'):
        return 0
    name, octv = s[:-1], int(s[-1])
    return 440 * 2 ** ((NOTE[name] + 12 * (octv + 1) - 69) / 12)


def music():
    bpm = 148
    step = 60 / bpm / 4  # 16th note
    lead = ("E5 . G5 . A5 . . G5 A5 . C6 . B5 . G5 . "
            "E5 . G5 . A5 . . G5 E5 . D5 . C5 . . . "
            "F5 . A5 . C6 . . A5 C6 . D6 . C6 . A5 . "
            "G5 . . E5 G5 . A5 . B5 . . . D6 . . . ").split()
    lead2 = ("C6 . B5 . A5 . G5 . A5 . . . E5 . G5 . "
             "A5 . G5 . E5 . D5 . E5 . . . . . . . "
             "F5 . G5 . A5 . C6 . D6 . C6 . A5 . G5 . "
             "A5 . B5 . C6 . . . C6 . . . - . . . ").split()
    bass_prog = ['C3', 'A2', 'F2', 'G2', 'C3', 'A2', 'F2', 'G2']
    total_steps = len(lead) + len(lead2)
    n = int(total_steps * step * SR)
    out = np.zeros(n)
    seq = lead + lead2

    def place(sig, st):
        s = int(st * step * SR)
        e = min(n, s + len(sig))
        out[s:e] += sig[:e - s]

    # lead with held notes
    i = 0
    while i < len(seq):
        if seq[i] not in ('.', '-'):
            j = i + 1
            while j < len(seq) and seq[j] == '.':
                j += 1
            d = (j - i) * step
            f = freq(seq[i])
            vib = f * (1 + 0.006 * np.sin(t(d) * 2 * np.pi * 6) * np.clip(t(d) / 0.15, 0, 1))
            sig = sq(vib, d, 0.25) * 0.22 * env(int(SR * d), a=0.004, r=0.05)
            echo = np.concatenate([np.zeros(int(SR * step * 3)), sig * 0.35])
            place(sig, i)
            place(echo, i)
            i = j
        else:
            i += 1
    # bass: octave bounce 8ths
    for bar in range(total_steps // 16):
        root = freq(bass_prog[bar % len(bass_prog)])
        for k in range(8):
            f = root * (2 if k % 2 else 1)
            d = step * 2
            place(tri(f, d) * 0.45 * env(int(SR * d), r=0.03), bar * 16 + k * 2)
        # chord stabs
        for k in (2, 6, 10, 14):
            for mult in (1.5 * 2, 1.26 * 2):
                d = step
                place(sq(root * mult, d, 0.5) * 0.05 * env(int(SR * d), r=0.05), bar * 16 + k)
    # drums
    for s in range(total_steps):
        if s % 8 == 0:
            d = 0.12
            f = np.linspace(160, 40, int(SR * d))
            place(tri(f, d) * 0.7 * np.exp(-t(d) * 25), s)
        if s % 8 == 4:
            d = 0.12
            place(noise(d) * 0.35 * np.exp(-t(d) * 22), s)
        if s % 2 == 0:
            d = 0.03
            place(np.random.RandomState(s).uniform(-1, 1, int(SR * d)) * 0.08 * np.exp(-t(d) * 90), s)
    out = np.tanh(out * 1.2)
    write('theme', out, 0.8)
    # results jingle
    notes = ['C5', 'E5', 'G5', 'C6', '.', 'B5', 'C6', 'D6', 'E6', '.', '.', '.']
    x = np.zeros(int(len(notes) * 0.12 * SR) + SR)
    for k, nn in enumerate(notes):
        if nn != '.':
            d = 0.12 if k < 8 else 0.8
            sig = sq(freq(nn), d, 0.25) * env(int(SR * d), r=0.1) * 0.4
            s = int(k * 0.12 * SR)
            x[s:s + len(sig)] += sig
    write('clear', x, 0.6)


sfx()
music()
print('audio done')
