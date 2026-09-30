"""Create the War God replica's original, deterministic 8-bit sound set.

Requires Python 3.12+ and NumPy. No downloads, samples, model calls or audio APIs.
Run from any directory: python tools/generate-mech-audio.py
"""
from __future__ import annotations

import hashlib
import json
import platform
from pathlib import Path
import wave

import numpy as np


ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "JumpNotIncluded/Assets/Art/Mech/Audio"
RATE = 44100
SEED = 20260928
TAU = np.pi * 2


def clock(seconds: float) -> np.ndarray:
    return np.arange(round(seconds * RATE), dtype=np.float64) / RATE


def phase(frequency: np.ndarray | float, size: int) -> np.ndarray:
    if np.isscalar(frequency):
        return np.arange(size) * float(frequency) * TAU / RATE
    return (np.cumsum(frequency) - frequency) * TAU / RATE


def pulse(angle: np.ndarray) -> np.ndarray:
    # Four odd harmonics retain the console-like edge without a harsh ideal square.
    return sum(np.sin(angle * h) / h for h in (1, 3, 5, 7)) / 1.2


def triangle(angle: np.ndarray) -> np.ndarray:
    return (np.sin(angle) - np.sin(3 * angle) / 9 + np.sin(5 * angle) / 25) / 1.06


def noise(size: int, seed: int, low: float = 250, high: float = 2400) -> np.ndarray:
    # FFT-filtered noise is periodic over its buffer, including the loop boundary.
    rng = np.random.default_rng(seed)
    spectrum = np.fft.rfft(rng.normal(size=size))
    hz = np.fft.rfftfreq(size, 1 / RATE)
    mask = np.exp(-((hz / high) ** 6)) * (1 - np.exp(-((hz / low) ** 4)))
    spectrum *= mask
    spectrum[0] = 0
    result = np.fft.irfft(spectrum, n=size)
    return result / max(1e-9, np.std(result)) / 3


def edge_window(size: int, attack: float, release: float) -> np.ndarray:
    result = np.ones(size)
    a, r = min(size, round(attack * RATE)), min(size, round(release * RATE))
    if a > 1:
        result[:a] *= .5 - .5 * np.cos(np.linspace(0, np.pi, a))
    if r > 1:
        result[-r:] *= .5 + .5 * np.cos(np.linspace(0, np.pi, r))
    return result


def finish(signal: np.ndarray, peak: float = .8, loop: bool = False) -> np.ndarray:
    # A small amplitude ladder supplies mild 8-bit grain. The output container stays PCM16.
    signal = np.round(signal * 128) / 128
    if loop:
        signal -= np.mean(signal)
    else:
        window = edge_window(len(signal), .008, min(.055, len(signal) / RATE / 3))
        signal *= window
        # Preserve zero endpoints while removing the one-shot's DC component.
        signal -= np.sum(signal) / np.sum(window) * window
    signal *= peak / max(1e-9, np.max(np.abs(signal)))
    pcm = np.rint(signal * 32767).astype(np.int16)
    if loop:
        # Choose a naturally quiet sample boundary; do not fade a repeating engine to silence.
        values = pcm.astype(np.int32)
        delta = values - np.roll(values, 1)
        curvature = np.abs(np.roll(delta, -1) - np.roll(delta, 1))
        boundary = int(np.argmin(np.abs(delta) * 8 + curvature))
        pcm = np.roll(pcm, -boundary)
    return pcm


def ignition() -> np.ndarray:
    t = clock(.4); progress = t / .4
    angle = phase(75 + 250 * progress ** 2, len(t))
    charge = (.18 + .65 * progress) * (.68 * triangle(angle) + .2 * pulse(angle * 2))
    catch = np.clip((t - .27) / .03, 0, 1) * np.exp(-np.maximum(t - .31, 0) * 13)
    return finish(charge + catch * (.3 * noise(len(t), SEED) + .25 * np.sin(TAU * 70 * t)))


def thrust_loop() -> np.ndarray:
    t = clock(1)
    modulation = .8 + .12 * np.sin(TAU * 14 * t) + .05 * np.sin(TAU * 7 * t)
    body = .47 * triangle(TAU * 84 * t) + .21 * np.sin(TAU * 42 * t)
    edge = .11 * pulse(TAU * 168 * t)
    flow = .22 * noise(len(t), SEED + 1, 180, 1800)
    return finish((body + edge + flow) * modulation, .68, loop=True)


def laser() -> np.ndarray:
    t = clock(.18)
    sweep = phase(180 + 920 * np.exp(-t * 23), len(t))
    body = .62 * triangle(sweep) + .15 * pulse(sweep)
    air = .15 * noise(len(t), SEED + 2, 450, 3100) * np.exp(-t * 34)
    return finish((body + air) * np.exp(-t * 15), .8)


def landing() -> np.ndarray:
    t = clock(.22)
    thump = .68 * np.sin(phase(42 + 80 * np.exp(-t * 28), len(t))) * np.exp(-t * 15)
    metal = (.13 * pulse(TAU * 330 * t) + .07 * np.sin(TAU * 487 * t)) * np.exp(-t * 35)
    return finish(thump + metal + .1 * noise(len(t), SEED + 3, 120, 1500) * np.exp(-t * 22), .8)


def shield() -> np.ndarray:
    t = clock(.12)
    angle = phase(220 + 520 * np.exp(-t * 30), len(t))
    body = .65 * triangle(angle) + .15 * np.sin(angle * 1.5)
    return finish((body + .08 * noise(len(t), SEED + 4, 550, 2300)) * np.exp(-t * 20), .74)


def descent() -> np.ndarray:
    t = clock(1.8)
    fall = np.clip(t / 1.2, 0, 1)
    brake = np.clip((t - 1.0) / .5, 0, 1)
    angle = phase(190 - 120 * fall + 115 * brake ** 2, len(t))
    engine = .43 * triangle(angle) + .13 * pulse(angle * 2)
    gas = noise(len(t), SEED + 5, 220, 1850) * (.16 + .26 * brake)
    tremolo = .86 + .1 * np.sin(TAU * 17 * t)
    return finish((engine + gas) * tremolo * (.3 + .7 * np.minimum(t / .5, 1)), .8)


def add_note(destination: np.ndarray, start: float, length: float, frequency: float, volume: float) -> None:
    t = clock(length)
    sound = (.78 * triangle(TAU * frequency * t) + .13 * pulse(TAU * frequency * t))
    sound *= edge_window(len(t), .006, .07) * np.exp(-t * 3) * volume
    offset = round(start * RATE)
    count = min(len(sound), len(destination) - offset)
    if count > 0:
        destination[offset:offset + count] += sound[:count]


def uplink() -> np.ndarray:
    t = clock(1.7); signal = np.zeros(len(t))
    for i, frequency in enumerate((196, 220, 261.6256, 293.6648, 349.2282, 392, 523.2511)):
        start = .025 + i * .205
        add_note(signal, start, .24, frequency, .62)
        add_note(signal, start + .072, .2, frequency, .16)
    return finish(signal, .76)


def ready() -> np.ndarray:
    signal = np.zeros(len(clock(.78)))
    for start, note in ((.012, 261.6256), (.205, 329.6276), (.398, 391.9954)):
        add_note(signal, start, .36, note, .65)
    return finish(signal, .78)


def boost() -> np.ndarray:
    t = clock(.15)
    angle = phase(95 + 330 * (1 - np.exp(-t * 19)), len(t))
    return finish((.54 * triangle(angle) + .13 * pulse(angle * 2) +
                   .2 * noise(len(t), SEED + 6, 300, 2000)) * np.exp(-t * 13), .78)


def write_wav(path: Path, pcm: np.ndarray) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(path), "wb") as stream:
        stream.setnchannels(1); stream.setsampwidth(2); stream.setframerate(RATE)
        stream.writeframes(pcm.astype("<i2").tobytes())


def analyze(path: Path, pcm: np.ndarray, loop: bool) -> dict:
    values = pcm.astype(np.float64) / 32768
    delta = np.diff(np.append(values, values[0]))
    result = {
        "file": path.relative_to(ROOT).as_posix(), "seconds": len(pcm) / RATE,
        "frames": len(pcm), "sample_rate": RATE, "channels": 1, "encoding": "PCM16",
        "peak": float(np.max(np.abs(values))), "rms": float(np.sqrt(np.mean(values ** 2))),
        "dc": float(np.mean(values)), "first_sample": int(pcm[0]), "last_sample": int(pcm[-1]),
        "loop": loop, "loop_seam_delta": float(abs(values[0] - values[-1])),
        "adjacent_delta_rms": float(np.sqrt(np.mean(delta ** 2))),
        "sha256": hashlib.sha256(path.read_bytes()).hexdigest(),
    }
    assert result["peak"] <= .85
    assert abs(result["dc"]) < 1e-4
    if loop:
        assert result["loop_seam_delta"] <= max(2 / 32768, result["adjacent_delta_rms"])
    else:
        assert pcm[0] == 0 and pcm[-1] == 0
    return result


def main() -> None:
    generators = [ignition, thrust_loop, laser, landing, shield, descent, uplink, ready, boost]
    report = {"python": platform.python_version(), "numpy": np.__version__, "seed": SEED,
              "generation": "Original offline procedural synthesis; no external audio samples or model calls.",
              "checks": "PCM16 mono 44100 Hz; peak <= 0.85; abs(DC) < 0.0001; zero one-shot endpoints; loop seam <= adjacent delta RMS.",
              "sounds": {}, "preview_order": []}
    preview = []; cursor = 0
    for generator in generators:
        name = generator.__name__.replace("_", "-")
        pcm = generator(); path = OUT / (name + ".wav")
        write_wav(path, pcm)
        report["sounds"][name] = analyze(path, pcm, name == "thrust-loop")
        audition = np.tile(pcm, 3) if name == "thrust-loop" else pcm
        if name == "thrust-loop":
            # Audition-only fades prevent clicking when entering/leaving the three-repeat demo.
            audition = np.rint(audition * edge_window(len(audition), .02, .035)).astype(np.int16)
        report["preview_order"].append({"sound": name, "start_seconds": cursor / RATE,
                                        "seconds": len(audition) / RATE})
        pause = np.zeros(round(.35 * RATE), dtype=np.int16)
        preview.extend((audition, pause)); cursor += len(audition) + len(pause)
    preview_path = ROOT / "artifacts/mech-audio-preview.wav"
    write_wav(preview_path, np.concatenate(preview))
    report["preview"] = preview_path.relative_to(ROOT).as_posix()
    report["preview_seconds"] = cursor / RATE
    analysis = ROOT / "work/mech-audio-analysis.json"
    analysis.parent.mkdir(parents=True, exist_ok=True)
    analysis.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    for name, entry in report["sounds"].items():
        print(f'{name:12} {entry["seconds"]:.2f}s  peak={entry["peak"]:.3f}  RMS={entry["rms"]:.3f}  DC={entry["dc"]:+.7f}  seam={entry["loop_seam_delta"]:.6f}')
    print(f"Preview: {preview_path} ({cursor / RATE:.2f}s)")


if __name__ == "__main__":
    main()
