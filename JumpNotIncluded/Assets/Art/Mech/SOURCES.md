# SL CHEATER crossover artwork

The original files listed below were copied unchanged on 2026-09-28 from the user's local SL Cheater project, at the user's request to introduce a purchasable replica of its mech. No raster edits or new image generation were performed for that original import. v1.14.0 adds a separately redrawn six-pose pixel sheet; it is not described as an unchanged SL Cheater source asset.

## Source and provenance

- `war-god-portrait.png`: `assets/art/war-god-pixel-v08/portrait.png`, 320 x 480 RGBA. The source README identifies it as the current War God / orbital assault chassis portrait, generated for SL Cheater from the previously approved design and prepared as pixel art. Its design has black and silver blade armor, cyan light seams, a gold antimatter core and folding wings.
- `war-god-flight-right.png`: `assets/map/war-god-flight-v07/right.png`, 1536 x 960 RGBA. The source project's accepted body art is reused with rigid wing/exhaust animation. The side views are intentionally bilateral; the right view is a mirror of the canonical left view.
- `war-god-walk-right.png`: `assets/map/war-god-walk-v05/right.png`, 1536 x 2880 RGBA. Distance-driven walking animation of the same approved body, with a fixed torso/head/wing assembly.
- `flight-animation.json` and `walk-animation.json` are unchanged copies of each source atlas's `animation.json`.

Source documentation: `assets/art/war-god-pixel-v08/README.md`, `assets/map/war-god-v04/README.md`, `docs/展翼修复v01.md`, and `docs/SL-CHEATER-Game-Design-Document-EN-v1.md` in the user's SL Cheater project. Reuse is authorized by the user for this crossover; these credits do not assert an open-source asset license.

The existing high-resolution promotional illustration remains `Assets/Art/Ads/slcheater-war-god.png` with its original credits. The imported portrait remains available for the premium showcase. The original atlases supplied the playable replica through v1.13.1; they remain as source and historical assets while the v1.14.0 playable replica and arrival use the new sheet below.

## v1.14.0 redrawn 8-bit replica

- `war-god-8bit-v1.png` is a new six-pose crossover redraw based on the SL Cheater War God design, retaining its dark silver armor, cyan accents, gold core and folding wings. The original imported art remains unchanged and available as reference/display material.
- Generated with the built-in imagegen tool, using the original flight atlas, Mario sheet and an in-game hover capture as references. The output PNG is preserved unchanged; the submitted prompt and runtime grid adaptation are recorded in [PIXEL-ART-PROMPT.md](PIXEL-ART-PROMPT.md).
- The sheet has three columns and two rows. Runtime poses are 0 idle, 1-2 walking, 3 expanded-wing hover, and 4-5 airborne horizontal cruise. This frame numbering belongs to the new sheet and replaces the historical frame selection below.
- Each pose is sampled on a 52 x 52 logical-pixel grid and rendered through `JumpNotIncluded/MechPixel` with a limited palette and hard alpha edges. Its effective density is 16 pixels per world unit, matching Mario and the terrain. The PNG's source resolution is not claimed as the effective in-game pixel density. The chassis and arrival use display scale 1.
- Vertical reaction jets and rear cruising trails are separate code-rendered effects, not baked into the new sheet. `MechExhaust` uses opaque white/cyan/blue squares at 1/16 world unit. Idle and ground walking emit neither trail; ascent/hover emits downward; horizontal cruise additionally emits opposite its direction of travel.
- `MechPixelLaser` uses a grid-stepped path with discrete palette changes and a pixel impact flash. Consciousness transfer, scanning, reactor feedback and ground wash use pixel blocks rather than smooth light ribbons or gradients.
- The real `big-idle` Mario sprite remains at its original 16 PPU with its original chroma-key material in the side-by-side QA fixture. The comparison adds only a visual reference, not a second player or a gameplay change.
- Prices, flight, landing, abyss protection, immunity, auto-lock damage and the 6.2-second unskippable arrival rules are unchanged. Imported audio is retained.

v1.14.0 已通过 226 项完整 Unity 运行检查、59 项推进专项检查、24 项登场画面检查和 15 项机甲商品页／飞行检查；保存 10 张推进图、24 张登场图和 14 张机甲图，Windows x64 构建成功。本次验证覆盖六帧像素造型、16 PPU 独立喷流、像素激光与意识接入，以及购买、落地、悬崖保护和两关通关；自动检查与布置的截图不替代人工试玩或课程验收录屏。

## v1.14.1 retro sound replacement

The active mech and arrival now use nine locally synthesized retro pixel-style cues: ignition, thrust-loop, laser, landing, shield, descent, uplink, ready and boost. Files are in `Audio/`, with exact durations, reproduction instructions and intended routing in [Audio/SOURCES.md](Audio/SOURCES.md). The deterministic offline generator is [tools/generate-mech-audio.py](../../../../tools/generate-mech-audio.py), using Python and NumPy to export mono 44,100 Hz PCM16 WAV files without external samples or model calls. Waveform analysis and the 11.50-second listening preview are separate verification artifacts. The original imported SL Cheater WAV files below remain unchanged as source/history, but no longer supply the active mech's runtime cues. The v1.14.0 visuals, gameplay, prices and 6.2-second arrival rules are unchanged.

v1.14.1 已通过 226 项完整 Unity 运行检查、59 项推进专项检查和 44 项机甲音频检查，Windows x64 构建成功。九种新音效的 WAV 格式、信号分析与源码 SHA256 已核对，另保留 11.50 秒分段试听与 6.2 秒真实 Unity 登场混音；原视觉截图及登场／商品页画面报告仍归属 v1.14.0，未声称重新执行。自动检查与试听素材不替代人工游玩或课程验收录屏。

## Historical imported crossover sound

These WAV files were copied unchanged from the same user's SL Cheater project and used through v1.14.0. They are retained as historical source assets:

- `war-god-charge.wav`: `assets/audio/thruster-v09/charge.wav`, the source War God's 0.35-second propulsion charge.
- `war-god-launch.wav`: `assets/audio/thruster-v09/launch.wav`, the source War God's selected ignition sound.
- `war-god-cruise.wav`: `assets/audio/thruster-v09/cruise.wav`, its quiet sustained propulsion loop.
- `war-god-replica-laser.wav`: `assets/audio/rail-bastion-v01/laser_single.wav`, the SL Cheater Rail Bastion's short laser pulse, reused as this replica edition's weapon sound.

The source sound documentation describes locally generated Stable Audio 3 Small-SFX audio followed by FFmpeg/NumPy/SoundFile editing; see `docs/推进器音效-v02.md`, the active `assets/audio/thruster-v09/profile.json`, and `docs/sfx-rail-bastion-v01.md`. No new audio generation, download, purchase, or API call was needed for this import. The source thruster profile recommends master gain -4 dB; charge and launch are one-shots, cruise loops. The laser source profile uses master gain -6 dB.

## Historical original-atlas integration

Both atlases use 192 x 192 frames, 8 columns, ordered left to right then top to bottom. Import with point filtering, no mipmaps, and preserve alpha. Do not fit or crop individual frames independently.

- Source flight animation metadata: frames 0-15 deploy over 0.35 seconds; frames 16-23 loop at 16 FPS; frames 24-39 retract over 0.40 seconds. The v1.13.1 platformer selection below intentionally chooses frames by movement state instead of running this full source sequence for every kind of flight.
- Walk: 120 total frames; 5 strength blocks of 24 phase frames. The highest-strength walk uses frames 96-119.
- The source map footprint is `[48,48,96,96]` with 3 x 3 map tiles. This is source metadata, not the Unity game's collision shape.
- A useful side-platformer foot anchor is `[96,140]` in each frame's top-left coordinate system (Unity normalized pivot `[0.5,0.27083333]`). Frame 0's visible body bounds are `[74,55,43,84]`; boost frame 16 extends left to x=32 for its wings/exhaust. The body anchor remains fixed across animation.

## Historical v1.13.1 propulsion and arrival integration

No raster files were edited for this correction. The original portrait, flight atlas, walking atlas and audio remain unchanged. The display scale is 1.18 for both the playable chassis and the arrival chassis.

- Ground idle selects original flight frame 0 with folded wings. Ground movement selects the walking atlas (`FlightFrame = -1` in the Unity controller). Both states have no exhaust, no flight wake and no looping engine sound.
- Vertical ascent and stationary hover select original flight frame 15: fully expanded wings without the sideways exhaust baked into the boost frames. A separate procedural cyan-white exhaust points down from the soles. Hover power is 0.38 so this supporting exhaust remains visibly active.
- Only airborne horizontal travel selects original flight frames 16-23, preserving the source's blue backward wake. The art and wake mirror together when facing left.
- The source animation defines horizontal heel mounts at `[93.48,127.96]` and `[101.13,118.03]` in the 192 x 192 frame's top-left coordinate system. Sending a downward effect from these horizontal mounts would bury part of it inside the boots. Unity therefore adds distinct vertical sole vents at `[93,133.5]` and `[104.5,137.5]`, derived from the lower transparent-pixel boundary of original frame 15. These vertical points are this project's integration choices, not original SL Cheater metadata. They are transformed through the displayed hull, including scale and horizontal mirroring. The separate exhaust is code-rendered geometry and light, not an altered atlas image.
- Descending onto actual ground or a pipe top shuts off exhaust and the engine loop. The minimum hover altitude is retained only as abyss protection; it does not prevent touching real support surfaces.
- The 6.2-second mandatory arrival also uses frame 15 at display scale 1.18 with the same sole-mounted cyan-white recoil. Orange fire and oversized rings have been removed; limited ground wash and dust remain anchored to the world ground. The consciousness-transfer light, sequence duration and completion rules are unchanged.

Historical v1.13.1 validation passed 226 complete Unity runtime checks, 46 propulsion checks and 24 arrival presentation checks, retaining 9 propulsion images and 24 arrival images, with a successful Windows x64 build. Those results do not validate the v1.14.0 redraw, pixel exhaust, laser or consciousness effects.

The earlier v1.13.0 report also had 226 checks and 24 arrival captures. It remains a separate historical result and must not be confused with v1.13.1 or the pending v1.14.0 validation.

"Moon Killer", "Starfield Camouflage" and the claim of acceleration to 1% of light speed are the user's requested satirical advertising copy for this game's replica edition. They are not being presented as technical benchmarks or a quotation of SL Cheater's existing game rules.
