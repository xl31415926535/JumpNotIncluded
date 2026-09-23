"""Prepare the Unity project from the supplied course assets and installed editor.
Only image format conversion is performed; sprite artwork remains unchanged.
"""
from pathlib import Path
import json, shutil, tarfile
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
PROJECT = ROOT / 'JumpNotIncluded'
EDITOR = Path('D:/Unity/Hub/Editor/6000.3.24f1/Editor')
for folder in ['Assets/Art', 'Assets/Audio', 'Assets/Scripts', 'Assets/Editor',
               'Assets/Resources', 'Assets/Scenes', 'Packages', 'ProjectSettings']:
    (PROJECT / folder).mkdir(parents=True, exist_ok=True)
assets = ROOT / 'lab-inspection/package'
for name, dest in [('f1bde3f95d8d049e59a4208ea4358c51', 'mario'),
                   ('7eb68ac47e5fe4bbca37c767951ce034', 'enemies'),
                   ('82bc45c4496284f4c823d1b837f65ec4', 'world')]:
    source = assets / name / 'asset'
    if source.exists():
        original = Image.open(source)
        original.convert('RGBA').save(PROJECT / 'Assets/Art' / (dest+'.png'))
        print(dest, original.size, original.convert('RGB').getpixel((0,0)))
    elif not (PROJECT / 'Assets/Art' / (dest+'.png')).exists():
        raise FileNotFoundError('Missing course sprite source: '+str(source))
for pathname in assets.glob('*/pathname'):
    logical = pathname.read_text(encoding='utf-8').strip()
    if logical.startswith('Assets/Sounds/'):
        shutil.copyfile(pathname.parent / 'asset', PROJECT/'Assets/Audio'/Path(logical).name)
package = EDITOR / 'Data/Resources/PackageManager/Editor/com.unity.inputsystem-1.20.0.tgz'
destination = PROJECT / 'Packages/com.unity.inputsystem'
if not (destination/'package.json').exists():
    destination.mkdir(parents=True, exist_ok=True)
    with tarfile.open(package) as archive:
        for entry in archive.getmembers():
            rel = Path(entry.name).relative_to('package') if entry.name.startswith('package/') else None
            if rel is None or entry.isdir() or '..' in rel.parts: continue
            target = destination / rel
            target.parent.mkdir(parents=True, exist_ok=True)
            source = archive.extractfile(entry)
            if source: target.write_bytes(source.read())
manifest = {'dependencies': {
    'com.unity.inputsystem': '1.20.0',
    'com.unity.test-framework': '1.6.0',
    'com.unity.ext.nunit': '2.0.5',
    'com.unity.modules.audio': '1.0.0',
    'com.unity.modules.imgui': '1.0.0',
    'com.unity.modules.imageconversion': '1.0.0',
    'com.unity.modules.jsonserialize': '1.0.0',
    'com.unity.modules.physics2d': '1.0.0',
    'com.unity.modules.uielements': '1.0.0',
    'com.unity.modules.animation': '1.0.0',
    'com.unity.modules.screencapture': '1.0.0'}}
(PROJECT/'Packages/manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
(PROJECT/'ProjectSettings/ProjectVersion.txt').write_text('m_EditorVersion: 6000.3.24f1\nm_EditorVersionWithRevision: 6000.3.24f1 (4e7b9b5b6244)\n',encoding='utf-8')
print('Prepared', PROJECT)
