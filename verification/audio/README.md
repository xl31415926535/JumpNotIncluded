# Mech audio verification / v1.14.1

The current Unity audio report is ../mech-audio-checks.txt (44 PASS). mech-audio-analysis.json records the nine original synthesized WAV files and their SHA-256 hashes; the matching generator is tools/generate-mech-audio.py in the source archive.

mech-audio-preview.wav is a 11.50-second ordered listening reel, not a live game recording. The engine clip repeats three times in this reel. See SOURCES.md for synthesis, routing and signal-check details.

arrival-mix.wav separately preserves 6.2 seconds of actual stereo Unity AudioRenderer output at 48000 Hz. arrival-timing.csv is copied from the capture frame sequence, and unity-capture-README.md retains its original provenance. The referenced frames stay in artifacts/mech-audio/frames and are excluded from the source archive; this staged capture is not a human playthrough or a course acceptance recording.

The screenshots and arrival/mech UI reports outside this folder retain their v1.14.0 version. They were not rerun or relabelled by this audio release packaging step.
