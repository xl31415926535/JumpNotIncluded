# Original War God replica sound set

Created for Jump Not Included on 2026-09-28 using deterministic, offline waveform synthesis. These sounds contain no downloaded recordings, existing game sounds, generated speech, music samples, or paid API output. This set is separate from the older SL CHEATER sound imports in the parent directory.

## Reproduce

From the repository root, run `python tools/generate-mech-audio.py` with Python 3.12+ and NumPy installed. The validated local environment is Python 3.12.14 and NumPy 2.3.5. The script uses NumPy for oscillators and band-limited noise, and Python's standard `wave` library for WAV export. It needs no network access, model download, account, or paid service.

The deterministic seed is `20260928`. Each file is mono, 44,100 Hz, signed 16-bit PCM. The synthesis uses a restrained pulse/triangle palette, low mechanical tones and filtered noise. The final files preserve PCM16 containers; modest amplitude quantization supplies the retro grain.

## Clips and intended routing

- `ignition.wav` — 0.40 s rising charge and engine catch; suggested World SFX gain 0.40–0.45.
- `thrust-loop.wav` — 1.00 s periodic bass/pulse engine and controlled jet noise; gain 0.12–0.16 while hovering, 0.22–0.28 during ascent. Loop this file; do not restart it every input frame.
- `laser.wav` — 0.18 s descending energy shot; suggested gain 0.45–0.55.
- `landing.wav` — 0.22 s low mechanical touchdown; suggested gain 0.45–0.50, once per air-to-ground transition.
- `shield.wav` — 0.12 s short absorbed-impact response; suggested gain 0.30–0.38, with a cooldown for repeated contact.
- `descent.wav` — 1.80 s falling orbital pitch that rises into braking thrust; suggested gain 0.40–0.45.
- `uplink.wav` — 1.70 s ascending consciousness-transfer arpeggio; suggested gain 0.38–0.45.
- `ready.wav` — 0.78 s three-note synchronization confirmation; suggested gain 0.45–0.50.
- `boost.wav` — 0.15 s brief acceleration pulse; suggested gain 0.28–0.35, once when cruising starts.

Suggested gains multiply the game's World SFX slider/mixer value. They are starting points for game mixing, not changes already made by this asset generator. Keep music on its existing separate music source.

## Preview and checks

`artifacts/mech-audio-preview.wav` plays the clips in the order listed above, with 0.35 s pauses. The engine loop repeats three times. Its preview segment has short entrance/exit fades; the actual loop asset has no silent fade or gap.

`work/mech-audio-analysis.json` records each file's duration, peak, RMS, DC offset, sample endpoints, loop seam difference and SHA-256. Generation asserts peak amplitude at most 0.85, absolute DC below 0.0001, and exact zero endpoints for every one-shot. The engine uses periodic oscillators/noise and chooses a naturally quiet cyclic boundary; its seam difference must not exceed the normal adjacent-sample RMS difference. These are signal checks, not a claim of subjective listening or in-game playback verification.
