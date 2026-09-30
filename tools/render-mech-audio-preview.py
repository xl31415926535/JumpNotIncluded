"""Assemble the actual Unity camera frames and captured mixer output, without dubbing."""
from pathlib import Path
import csv
import subprocess
import wave

root = Path(__file__).resolve().parents[1]
capture = root / 'artifacts/mech-audio'
frames = capture / 'frames'
sound = capture / 'arrival-mix.wav'
with wave.open(str(sound), 'rb') as stream:
    duration = stream.getnframes() / stream.getframerate()
rows = list(csv.DictReader((frames / 'timing.csv').open(encoding='utf-8')))
rows = [row for row in rows if float(row['audio_seconds']) < duration]
assert rows and float(rows[0]['audio_seconds']) == 0
lines = []
for index, row in enumerate(rows):
    name = row['file']
    assert (frames / name).is_file() and name.startswith('frame_')
    stop = float(rows[index + 1]['audio_seconds']) if index + 1 < len(rows) else duration
    length = stop - float(row['audio_seconds'])
    assert length > 0
    lines += [f"file '{name}'", f'duration {length:.6f}']
lines += [f"file '{rows[-1]['file']}'"]
listing = frames / 'audio-preview.ffconcat'
listing.write_text('\n'.join(lines) + '\n', encoding='utf-8')
output = root / 'artifacts/arrival-preview-with-audio.mp4'
subprocess.run([
    'D:/AiWorkFlow/AudioTools/ffmpeg/ffmpeg.exe', '-hide_banner', '-loglevel', 'error', '-y',
    '-f', 'concat', '-safe', '0', '-i', listing.as_posix(), '-i', sound.as_posix(),
    '-vf', 'fps=30', '-c:v', 'h264_mf', '-b:v', '3500k', '-pix_fmt', 'yuv420p',
    '-c:a', 'aac', '-b:a', '160k', '-t', f'{duration:.6f}', '-movflags', '+faststart', output.as_posix()
], check=True)
print(f'{output}: {len(rows)} real Unity frames, {duration:.2f}s of actual captured stereo audio')
