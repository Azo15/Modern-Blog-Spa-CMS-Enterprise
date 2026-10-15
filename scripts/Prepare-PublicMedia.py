"""Recreate checked-in WebP editorial assets from their licensed Pexels source manifest.

Optional development utility: Python 3 and Pillow are required. No network is used by the running app.
Run from the repository root. Existing assets are kept unless --refresh is supplied.
"""
from pathlib import Path
import argparse
import concurrent.futures
import hashlib
import io
import json
import urllib.request
from PIL import Image, ImageOps

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--refresh', action='store_true', help='Download and encode existing editorial assets again')
options = parser.parse_args()
webroot = Path(__file__).resolve().parents[1] / 'WebUI' / 'wwwroot'
manifest_path = webroot / 'img' / 'media-sources.json'
manifest = json.loads(manifest_path.read_text(encoding='utf-8'))
assets = manifest['assets']
if len({asset['photoId'] for asset in assets}) != len(assets):
    raise ValueError('Editorial assets must use independent source photographs.')

def prepare(asset):
    destination = webroot / asset['file']
    if options.refresh or not destination.exists():
        request = urllib.request.Request(asset['download'], headers={'User-Agent': 'Mozilla/5.0'})
        with urllib.request.urlopen(request, timeout=60) as response:
            original = response.read()
        image = ImageOps.exif_transpose(Image.open(io.BytesIO(original))).convert('RGB')
        image.thumbnail((2200, 1500) if destination.stem.startswith('hero') else (1200, 1300))
        destination.parent.mkdir(parents=True, exist_ok=True)
        image.save(destination, 'WEBP', quality=82, method=6)
        asset.update(width=image.width, height=image.height, bytes=destination.stat().st_size)
    asset['sha256'] = hashlib.sha256(destination.read_bytes()).hexdigest()
    return asset

with concurrent.futures.ThreadPoolExecutor(max_workers=6) as pool:
    prepared = list(pool.map(prepare, assets))
if len({asset['sha256'] for asset in prepared}) != len(prepared):
    raise ValueError('Duplicate photograph content was detected.')
manifest.update(assets=prepared, errors=[])
manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding='utf-8')
print(f'{len(prepared)} independent WebP photographs verified.')
