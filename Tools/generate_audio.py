import wave
from pathlib import Path

import numpy as np

REPO = Path(__file__).resolve().parents[1]
OUT = REPO / "Assets" / "_Project" / "Audio"
SR = 44100
rng = np.random.default_rng(7)


def t(seconds):
    return np.arange(int(SR * seconds)) / SR


def env(n, attack=0.005, decay=0.2, curve=4.0):
    x = np.arange(n) / SR
    a = np.clip(x / max(attack, 1e-5), 0, 1)
    d = np.exp(-curve * np.clip(x - attack, 0, None) / max(decay, 1e-5))
    return a * d


def sine(freq, seconds, phase=0.0):
    return np.sin(2 * np.pi * freq * t(seconds) + phase)


def sweep(f0, f1, seconds, shape=1.0):
    x = t(seconds)
    k = (x / seconds) ** shape
    freq = f0 + (f1 - f0) * k
    return np.sin(2 * np.pi * np.cumsum(freq) / SR)


def noise(seconds):
    return rng.uniform(-1, 1, int(SR * seconds))


def lowpass(x, cutoff):
    a = np.exp(-2 * np.pi * cutoff / SR)
    y = np.empty_like(x)
    acc = 0.0
    for i in range(len(x)):
        acc = (1 - a) * x[i] + a * acc
        y[i] = acc
    return y


def highpass(x, cutoff):
    return x - lowpass(x, cutoff)


def mix_at(dst, src, at_seconds, gain=1.0):
    i = int(at_seconds * SR)
    n = min(len(src), len(dst) - i)
    if n > 0:
        dst[i:i + n] += src[:n] * gain
    return dst


def fade_out(x, seconds=0.01):
    n = min(len(x), int(SR * seconds))
    x[-n:] *= np.linspace(1, 0, n)
    return x


def save(path, x, peak=0.85, fade=True):
    x = np.asarray(x, dtype=np.float64)
    m = np.max(np.abs(x))
    if m > 0:
        x = x / m * peak
    if fade:
        x = fade_out(x)
    path.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(path), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes((x * 32767).astype(np.int16).tobytes())


def tone(freq, seconds, harmonics=(1.0,), decay=0.3, attack=0.003, curve=4.0):
    n = int(SR * seconds)
    x = sum(a * sine(freq * (i + 1), seconds) for i, a in enumerate(harmonics))
    return x * env(n, attack, decay, curve)


def note(semitones_from_a4):
    return 440.0 * 2 ** (semitones_from_a4 / 12)


def sfx_shoot():
    n = int(SR * 0.11)
    zap = sweep(1500, 420, 0.11, 0.6) * env(n, 0.001, 0.05, 5)
    hiss = highpass(noise(0.11), 4000) * env(n, 0.001, 0.015, 6) * 0.25
    return zap + hiss


def sfx_turret():
    n = int(SR * 0.12)
    twang = sweep(700, 300, 0.12, 0.4) * env(n, 0.001, 0.04, 5)
    click = highpass(noise(0.12), 2500) * env(n, 0.0005, 0.008, 6) * 0.5
    return twang * 0.7 + click


def sfx_cannon():
    n = int(SR * 0.5)
    boom = sweep(130, 45, 0.5, 0.5) * env(n, 0.002, 0.22, 4)
    rumble = lowpass(noise(0.5), 300) * env(n, 0.002, 0.18, 4) * 2.5
    ring = tone(1320, 0.5, (1.0, 0.0, 0.3), decay=0.25) * 0.12
    return boom + rumble + ring


def sfx_enemy_die():
    n = int(SR * 0.16)
    body = sweep(320, 90, 0.16, 0.7) * env(n, 0.001, 0.06, 5)
    crunch = lowpass(noise(0.16), 1800) * env(n, 0.001, 0.035, 5) * 0.9
    return body + crunch


def sfx_pickup():
    x = np.zeros(int(SR * 0.16))
    mix_at(x, tone(note(15), 0.12, (1.0, 0.0, 0.25), decay=0.06), 0.0, 0.8)
    mix_at(x, tone(note(22), 0.12, (1.0, 0.0, 0.25), decay=0.07), 0.04, 0.7)
    return x


def sfx_drain():
    return tone(note(19), 0.05, (1.0, 0.3), decay=0.015, curve=5)


def sfx_build():
    x = np.zeros(int(SR * 0.7))
    n = int(SR * 0.2)
    mix_at(x, sweep(160, 60, 0.2) * env(n, 0.002, 0.08, 4), 0.0, 1.0)
    for i, semis in enumerate((3, 7, 10, 15)):
        mix_at(x, tone(note(semis), 0.6, (1.0, 0.3, 0.15), decay=0.25), 0.03 + i * 0.045, 0.35)
    return x


def sfx_upgrade():
    x = np.zeros(int(SR * 0.6))
    for i, semis in enumerate((3, 10, 15, 22)):
        mix_at(x, tone(note(semis), 0.35, (1.0, 0.0, 0.3), decay=0.12), i * 0.06, 0.45)
    return x


def sfx_wave_start():
    seconds = 1.4
    n = int(SR * seconds)
    shape = np.minimum(1.0, t(seconds) / 0.35) * np.exp(-1.8 * np.clip(t(seconds) - 0.35, 0, None))
    base = note(-31)
    horn = sum(a * sine(base * h, seconds) for h, a in ((1, 1.0), (2, 0.5), (3, 0.35), (4, 0.2), (5, 0.12)))
    fifth = sum(a * sine(base * 1.5 * h, seconds) for h, a in ((1, 0.5), (2, 0.25)))
    return (horn + fifth) * shape[:n]


def sfx_core_hit():
    n = int(SR * 0.22)
    return sweep(110, 50, 0.22) * env(n, 0.002, 0.08, 4) + lowpass(noise(0.22), 500) * env(n, 0.001, 0.05, 4) * 1.5


def sfx_click():
    n = int(SR * 0.06)
    return sweep(900, 1300, 0.06) * env(n, 0.001, 0.02, 5)


def sfx_win():
    x = np.zeros(int(SR * 1.8))
    for i, semis in enumerate((3, 7, 10, 15)):
        mix_at(x, tone(note(semis), 0.5, (1.0, 0.3, 0.1), decay=0.2), i * 0.12, 0.5)
    for semis in (3, 10, 15, 19):
        mix_at(x, tone(note(semis), 1.3, (1.0, 0.25, 0.1), decay=0.55, curve=3), 0.5, 0.3)
    return x


def sfx_lose():
    x = np.zeros(int(SR * 1.8))
    for i, semis in enumerate((3, -2, -6, -9)):
        mix_at(x, tone(note(semis), 0.7, (1.0, 0.4, 0.2), decay=0.3), i * 0.22, 0.5)
    n = int(SR * 1.0)
    mix_at(x, sweep(90, 40, 1.0) * env(n, 0.01, 0.5, 3), 0.7, 0.8)
    return x


def music_loop():
    bpm = 80.0
    beat = 60.0 / bpm
    bars = 8
    seconds = bars * 4 * beat
    total = int(SR * seconds)
    x = np.zeros(total)
    time = t(seconds)

    breath = 0.6 + 0.4 * np.sin(2 * np.pi * time / (8 * beat))
    d2 = note(-31)
    drone = (sine(d2, seconds) + 0.5 * sine(d2 * 1.5, seconds) + 0.25 * sine(d2 * 2, seconds)) * breath
    x += drone * 0.22

    roots = [-31, -31, -28, -33]
    for b in range(bars * 4):
        f = note(roots[(b // 4) % len(roots)] + 12)
        n = int(SR * beat)
        pulse = (sine(f, beat) + 0.3 * sine(f * 2, beat)) * env(n, 0.01, beat * 0.35, 4)
        mix_at(x, pulse, b * beat, 0.18)

    pattern = [17, None, 24, None, 20, 29, None, 24, 17, None, 22, None, 20, None, 27, None]
    for i in range(bars * 8):
        semis = pattern[i % len(pattern)]
        if semis is None:
            continue
        mix_at(x, tone(note(semis), 0.9, (1.0, 0.0, 0.2), decay=0.35, curve=3), i * beat / 2, 0.08)

    return x


def main():
    sfx = {
        "Shoot": sfx_shoot, "Turret": sfx_turret, "Cannon": sfx_cannon, "EnemyDie": sfx_enemy_die,
        "Pickup": sfx_pickup, "Drain": sfx_drain, "Build": sfx_build, "Upgrade": sfx_upgrade,
        "WaveStart": sfx_wave_start, "CoreHit": sfx_core_hit, "Click": sfx_click, "Win": sfx_win, "Lose": sfx_lose,
    }
    for name, make in sfx.items():
        save(OUT / "SFX" / f"{name}.wav", make())
        print("sfx", name)

    loop = music_loop()
    save(OUT / "Music" / "Ambient.wav", loop, peak=0.7, fade=False)
    print("music Ambient")


if __name__ == "__main__":
    main()
