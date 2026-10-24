"""Package actual Unity lift frames into a GIF using their recorded game times.

Usage: python make_lift_preview.py CAPTURES_DIR OUTPUT.gif
Requires Pillow in the development Python environment; does not alter scene content.
"""
import csv
import sys
from pathlib import Path
from PIL import Image

capture_dir = Path(sys.argv[1]).resolve()
output = Path(sys.argv[2]).resolve()
with (capture_dir / 'lift-animation-times.csv').open(encoding='utf-8', newline='') as stream:
    samples = [(name, float(time)) for name, time in csv.reader(stream) if name]
if len(samples) < 3:
    raise RuntimeError('Not enough recorded lift frames.')
if any(b[1] < a[1] for a, b in zip(samples, samples[1:])):
    raise RuntimeError('Game timestamps must be monotonic.')
frames = []
for name, _ in samples:
    with Image.open(capture_dir / (name+'.png')) as source:
        frames.append(source.convert('RGB'))
durations = [max(20, round((b[1]-a[1])*1000)) for a, b in zip(samples, samples[1:])]
durations.append(1500)  # Hold the open upper landing before the preview loops.
output.parent.mkdir(parents=True, exist_ok=True)
frames[0].save(output, format='GIF', save_all=True, append_images=frames[1:],
               duration=durations, loop=0, disposal=2, optimize=True)
print(f'{len(frames)} Unity frames, {samples[-1][1]-samples[0][1]:.2f}s recorded travel, {output.stat().st_size} bytes')
