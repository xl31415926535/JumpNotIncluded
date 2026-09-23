from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED
import re

root = Path.cwd()
project = root / 'JumpNotIncluded'
artifacts = root / 'artifacts'
report = (artifacts / 'unity-runtime-checks.txt').read_text(encoding='utf-8')
assert report.endswith('ALL RUNTIME CHECKS PASSED\n')
assert report.count('PASS: ') == 145
assert 'revision: 9' in (project / 'Assets/Resources/GameAssets.asset').read_text()
for path in list((project / 'Assets/Scripts').glob('*.cs')) + list((project / 'Assets/GameData/Products').glob('*.asset')):
    text = path.read_text(encoding='utf-8')
    assert not re.search(r'[\u3400-\u9fff]|\\u[4-9a-fA-F][0-9a-fA-F]{3}', text), f'Non-English text: {path}'

source_zip = artifacts / 'JumpNotIncluded-Unity-Source.zip'
with ZipFile(source_zip, 'w', ZIP_DEFLATED, compresslevel=6) as archive:
    for folder in ('Assets', 'Packages', 'ProjectSettings'):
        for path in sorted((project / folder).rglob('*')):
            if path.is_file():
                archive.write(path, path.relative_to(root).as_posix())
    for name in ('JumpNotIncluded/README.md', 'docs/jump-not-included-design.md', 'tools/check-project.ps1', 'tools/build-unity.ps1', 'tools/prepare-project.py'):
        archive.write(root / name, name)
    archive.write(artifacts / 'unity-runtime-checks.txt', 'verification/unity-runtime-checks.txt')
    archive.write(artifacts / 'top-up-ui-checks.txt', 'verification/top-up-ui-checks.txt')
    archive.write(artifacts / 'receipt-ui-checks.txt', 'verification/receipt-ui-checks.txt')
    for path in sorted((artifacts / 'previews').glob('*.png')):
        archive.write(path, 'verification/previews/' + path.name)

windows_zip = artifacts / 'JumpNotIncluded-Windows.zip'
build = project / 'Builds/Windows'
with ZipFile(windows_zip, 'w', ZIP_DEFLATED, compresslevel=6) as archive:
    for path in sorted(build.rglob('*')):
        if path.is_file():
            archive.write(path, 'JumpNotIncluded-Windows/' + path.relative_to(build).as_posix())
    archive.writestr('JumpNotIncluded-Windows/README.txt', '''Jump Not Included - v1.10.2
Extract the entire folder, then run JumpNotIncluded.exe.
Choose 1 PLAYER GAME, then START.

v1.10.2: growth mushrooms use the correct red sprite. Stars play the looping Starman BGM, pause with the game, and resume the level track at its saved position when the effect expires. Repeated stars do not restart the music; mute remains respected.

Move: A/D or arrow keys. Jump: SPACE or W. Fire: J. Pause: ESC.
Music and sound effects have separate switches in the menus.
All game text is English. Shop and ads appear only after death.
Free checkpoint retry is always available. All payments are simulated.

Revive ad: watch 2 seconds to return to the checkpoint, without cash.
Cash ad: earn $1 for each full second; stop any time. Cash cap: $99.
Mario, SUTD/AI and SL Cheater ads play in shuffled rounds of three.
Cash ads change creative every 5 seconds without resetting rewards.
Ads pause when the game loses focus. Artwork is bundled for offline use.

Cash packs: $1.00 -> 100 coins; $9.80 -> 1000; $18.80 -> 2000.
In the upgrade shop, click + beside the coin balance at the top left.
The dedicated COIN TOP-UP page shows three coin packs and your ad cash.
Watch ads there to earn cash, then click a pack price to exchange it.
BACK TO UPGRADES, X or ESC returns to the shop while gameplay stays paused.
Each map coin adds one spendable coin and 100 score points.
Jump DLC: 199 coins. Fire Flower: 299. Mushroom ID: 1299.
Master Guide: 1999. Monarch Wings: 2999. Gatling: 5999.
Fire Flower transforms you immediately; no flowers appear on the maps.
Mushroom ID makes every poison mushroom safe, including future spawns.
Master Guide shows hidden-block outlines and their reward icons.
Monarch Wings include basic jumping and one extra midair jump.
Gatling: hold J to clear a 14-tile corridor ahead, including hidden bricks above your gun. It demolishes pipes, removes poison and enemies, and paves gaps into level ground.
Fire Flower refunds in World 1-2 return the actual coin price paid.

World 1 begins without hidden blocks. The first cliff is now six tiles wide, with eight hidden blocks in two tiers.
Both late and early first-visit jumps meet an ambush. Coins and stars hide in the trap.
World 2 begins with ten 18-health Bowsers who independently patrol and fire at you.
The hostile corridors contain 29 Goombas and 31 poison mushrooms.
Normal fireballs and stomps chip away at each boss; Gatling erases all bosses in its paid corridor.
World 2 checkpoints are after the battalion at tile 34 and after the pit at tile 80.
Damage: Fire Mario -> Super Mario -> Small Mario -> death.
Each downgrade grants 1.5 seconds of protection. Pit falls remain fatal.
Checkpoint saves preserve cash, coins, upgrades and recorded world progress.

Both victory screens show ad seconds, gross coins spent and hands-on control seconds side by side.
Ad time includes unrewarded fractions; inactive windows do not add time.
Hands-on time counts move/jump/fire input during play; idle time, menus and ads do not count.
Refunds, net coin spending and a time-share bar expose the real cost of the run.
Failed attempts and both worlds contribute to the totals. Restarting clears them.

Built with Unity 6000.3.24f1. 145 automated Play Mode checks passed.
UI verified through Unity rendering at 1280x720, 960x540 and 1024x768.
Verified: buy only Gatling, hold right and J, and clear BOTH complete worlds without jumping or additional deaths. Manual playtesting and a classroom recording remain outstanding.
''')
for path in (source_zip, windows_zip):
    with ZipFile(path) as archive:
        assert archive.testzip() is None
        print(f'{path.name}: {len(archive.namelist())} files, {path.stat().st_size / 1048576:.1f} MiB; integrity OK')
with ZipFile(source_zip) as archive:
    for name in ('Assets/Scripts/BossActor.cs', 'Assets/Scripts/BossFlame.cs', 'Assets/Scripts/BlockActor.cs', 'Assets/Scripts/Fireball.cs', 'Assets/Art/monarch-wings.png', 'Assets/Scripts/GameUI.cs', 'Assets/Scripts/SceneRoot.cs', 'Assets/Scripts/PlayerMotor.cs', 'Assets/Scripts/PlayerFormController.cs', 'Assets/Scripts/WorldBuilder.cs', 'Assets/Scripts/RunModel.cs', 'Assets/Scripts/RunState.cs', 'Assets/Editor/RuleChecks.cs', 'Assets/Editor/RuntimeChecks.cs', 'Assets/Resources/GameAssets.asset', 'Assets/Art/Ads/slcheater-war-god.png', 'Assets/Art/Ads/sutd-ai-mindset.jpg', 'Assets/Art/Ads/sutd-school-for-innovators.jpg'):
        assert archive.read('JumpNotIncluded/' + name) == (project / name).read_bytes()
with ZipFile(windows_zip) as archive:
    name = 'JumpNotIncluded_Data/Managed/Assembly-CSharp.dll'
    assert archive.read('JumpNotIncluded-Windows/' + name) == (build / name).read_bytes()
print('Packages contain the current source, assets, and compiled game.')



combined = root / 'JumpNotIncluded.zip'
with ZipFile(combined, 'w', ZIP_DEFLATED, compresslevel=6) as archive:
    with ZipFile(source_zip) as original:
        for name in original.namelist():
            archive.writestr(name, original.read(name))
    with ZipFile(windows_zip) as original:
        for name in original.namelist():
            archive.writestr(name.replace('JumpNotIncluded-Windows/', 'JumpNotIncluded/Builds/Windows/', 1), original.read(name))
with ZipFile(combined) as archive:
    assert archive.testzip() is None
    assert archive.read('JumpNotIncluded/Assets/Scripts/EnemyActor.cs') == (project/'Assets/Scripts/EnemyActor.cs').read_bytes()
    assert archive.read('JumpNotIncluded/Builds/Windows/JumpNotIncluded_Data/Managed/Assembly-CSharp.dll') == (build/'JumpNotIncluded_Data/Managed/Assembly-CSharp.dll').read_bytes()
    print(f'{combined.name}: {len(archive.namelist())} files, {combined.stat().st_size / 1048576:.1f} MiB; integrity OK')
