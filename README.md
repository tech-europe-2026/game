# SKYROLL

A minimal, physics-driven 2D ball game built in **Unity 2022.3 LTS** and shipped to the browser with **WebGL**.
You control a glossy ball that rolls, jumps, dashes, grows, flips gravity and climbs across floating white platforms in a soft gradient sky: rails, roller-coasters, glass tubes, ice slides, pulsing lasers and a final boss duel against a red rival ball.

- **Play now (desktop or iPhone):** https://slavastar.itch.io/game
- **Engine:** Unity 2022.3.45f1 (C#), built-in 2D physics (Box2D)
- **Target:** WebGL (desktop browsers + mobile Safari/Chrome, landscape)
- **Deeper docs for the jury:** [`docs/TECHNICAL.md`](docs/TECHNICAL.md)

---

## Highlights

| Feature | What it is |
|---|---|
| Stylish physics | Rigidbody2D ball with squash & stretch, spin tied to angular speed, coyote time, jump buffering, hit-stop, screen shake and particle bursts |
| 15 ball states | Each ability swaps the ball sprite (idle, spin, dash, grow, freeze, stun, reverse, climb, …) extracted from our own concept art (`assets/states.jpeg`) |
| 5 levels | `FIRST FLIGHT`, `UPSIDE`, `HEAVY WEATHER`, `PULSE`, and the boss level `DUEL` — every level has its own sky gradient and music |
| Level menu | Pick any level from the title screen (click/tap a card or press `1`–`5`) |
| Obstacles | Rails & roller-coasters, transparent glass tubes, ice (no steering), launch pads, spinners, shards, glass, crates, moving platforms, turrets |
| Lasers | **Red** lasers pulse on/off and cost a heart; **blue** lasers pulse the same way but only push the ball back |
| Boss duel | One fixed screen, you vs. a red ball, 3 lives each. Ramming the other ball harder removes one of its lives. The rival shoots, dashes, grows heavy and teleports |
| Music | Calm procedurally-synthesised loop per level (no audio files), with a MUSIC/MUTED toggle that is remembered |
| Mobile | Fixed ◀ ▶ touch pads, big JUMP button, context-sensitive ability buttons, rotate-to-landscape screen |

## Controls

| Action | Keyboard | Touch |
|---|---|---|
| Roll | `A` / `D` or ← / → | hold ◀ / ▶ (lower-left) |
| Jump | `Space` / `W` / ↑ | JUMP |
| Crouch | `S` / ↓ | CROUCH (hold) |
| Dash | `Shift` | DASH |
| Grow (heavy) | `G` | GROW |
| Reverse gravity | `E` | REVERSE |
| Climb walls | hold `C` | CLIMB (hold) |
| Restart level | `R` | ↻ (top-right) |
| Level menu | `L` / `Esc` / MENU button | MENU |
| Music on/off | `M` / MUSIC button | MUSIC |
| Pick level (menu) | `1`–`5`, ←/→ + `Enter` | tap a card |

Only the abilities a level needs are shown on screen (e.g. level 3 is just JUMP + GROW).

---

## Repository layout

```
.
├── unity/                     Unity project (open this folder in Unity Hub)
│   ├── Assets/Scripts/        All gameplay code (C#), see docs/TECHNICAL.md
│   ├── Assets/Editor/         Build script (BuildWebGL.cs) + sprite import rules
│   ├── Assets/Resources/Sprites/  Ball-state sprites (blue player + red rival)
│   ├── Assets/WebGLTemplates/Sky/ Custom mobile-friendly HTML page template
│   ├── Packages/manifest.json Built-in modules only (no third-party packages)
│   └── ProjectSettings/       Unity 2022.3.45f1 project settings
├── webgl/                     Latest committed WebGL build (what itch.io serves)
├── assets/                    Source concept art (states.jpeg) + extracted sprites
├── .github/workflows/deploy.yml  CI: push webgl/ to itch.io with Butler on main
├── docs/TECHNICAL.md          Architecture & technical documentation
└── index.html, game.js, …     Earlier Kaboom.js prototype (not used by Skyroll)
```

---

## Setup & installation

### 1. Just play it

Open https://slavastar.itch.io/game. On iPhone use Safari, tap **Run game**, then the fullscreen button, and hold the phone sideways. *Share → Add to Home Screen* gives it an app-like icon.

### 2. Run the committed build locally (no Unity needed)

The `webgl/` folder is a ready-to-serve build. Browsers block WebGL builds from `file://`, so serve it over HTTP:

```bash
git clone https://github.com/tech-europe-2026/game.git
cd game/webgl
python3 -m http.server 8091        # or: npx serve -l 8091 .
# open http://localhost:8091
```

### 3. Open and edit in Unity

1. Install **Unity Hub** and editor **2022.3.45f1** with the **WebGL Build Support** module.
2. Sign in and activate a (free) Personal license in Unity Hub.
3. *Add project from disk* → select the `unity/` folder.
4. Menu **Skyroll → Build WebGL** builds into `../webgl`.
   There is no scene to hand-edit: the build script generates `Assets/Scenes/Main.unity` with just a camera, and `GM` builds everything else from code at runtime.

To play in the editor, open `Assets/Scenes/Main.unity` (created by the first build) and press Play.

### 4. Build from the command line (headless / CI)

```bash
cd unity
<path-to>/Unity/Hub/Editor/2022.3.45f1/Editor/Unity \
  -batchmode -nographics -quit \
  -projectPath . -buildTarget WebGL \
  -executeMethod BuildWebGL.Build \
  -logFile build.log
grep "BUILD RESULT" build.log        # -> BUILD RESULT: Succeeded size=…
```

Output: `webgl/index.html`, `webgl/Build/*.unityweb` (gzip, with decompression fallback so any static host works).

### 5. Deploy to itch.io

Manual, with [butler](https://itch.io/docs/butler/):

```bash
export BUTLER_API_KEY=...            # https://itch.io/user/settings/api-keys
butler push webgl slavastar/game:html
butler status slavastar/game:html
```

Automatic: `.github/workflows/deploy.yml` zips `webgl/` and pushes it with `manleydev/butler-publish-itchio-action` on every push to `main` (needs the `BUTLER_API_KEY` repository secret).

itch.io page settings: *Kind of project* = HTML, tick *This file will be played in the browser*, viewport 1280×720, *Mobile friendly* (landscape), fullscreen button on.

---

## Tech stack (summary)

| Layer | Tool / API |
|---|---|
| Engine | Unity 2022.3.45f1 LTS, C# (.NET Standard 2.1), IL2CPP → WebAssembly |
| Physics | Unity Physics 2D (Box2D): `Rigidbody2D`, `CircleCollider2D`, `EdgeCollider2D`, `PolygonCollider2D`, `BoxCollider2D`, `HingeJoint2D`, `PhysicsMaterial2D`, `Physics2D.OverlapCircle` queries |
| Rendering | `SpriteRenderer` + procedurally generated textures (`Texture2D`, `Sprite.Create`), `Sprites/Default` shader, 9-sliced rounded platforms |
| UI | Unity IMGUI (`OnGUI`, `GUIStyle`) for HUD, menu, touch buttons |
| Input | `UnityEngine.Input` (keyboard, mouse, multi-touch) behind a small `Controls` abstraction |
| Audio | `AudioSource`, `AudioClip.Create` + `SetData`: all SFX and music are synthesised at runtime |
| Persistence | `PlayerPrefs` (music on/off) |
| Web | Custom WebGL template (`WebGLTemplates/Sky`), gzip compression |
| Art pipeline | Python 3 + Pillow + NumPy to cut ball states from concept art and hue-shift the red rival |
| Distribution | itch.io + butler, GitHub Actions |

Details on every system, the physics model, level format, boss AI and audio synthesis are in **[docs/TECHNICAL.md](docs/TECHNICAL.md)**.
