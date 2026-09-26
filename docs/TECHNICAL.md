# SKYROLL: technical documentation

This document is for reviewers who want to see how the game works under the hood: architecture, the frameworks and APIs used, the physics model, how levels are authored, the boss AI, the audio synthesis, the build and deployment pipeline, and the known limits.

---

## 1. Architecture at a glance

Skyroll is **code-first**. The only scene (`Assets/Scenes/Main.unity`) holds nothing but a camera, and the build script regenerates it on every build. When the game starts, `GM` (the game manager) creates every object from C#: sky, clouds, platforms, obstacles, the ball, HUD and audio. Nothing is placed by hand in the editor, so the whole game can be reviewed as plain text diffs.

```
            ┌───────────── GM (MonoBehaviour, singleton) ─────────────┐
            │ mode: Title → Playing → LevelDone → Won                 │
            │ LoadLevel(i) · StartLevel(i) · ToMenu() · OnGUI HUD     │
            └──┬──────────┬──────────┬──────────┬──────────┬──────────┘
               │          │          │          │          │
         Controls      Levels   LevelBuilder   Ball      CameraFollow
     (keys + touch)   (data)  (geometry API) (player)  (follow/fixed view,
               │          │          │          │        parallax sky)
               │          └────►  Tile, Mover, Spinner, Turret, PadPower,
               │                  Bullet, Boss (runtime components)
               │
          Fx (particles, shake, hit-stop) · Gfx (procedural sprites)
          Sfx (synth one-shots) · Music (synth loops)
```

| File | Responsibility |
|---|---|
| `GM.cs` | Bootstraps the systems, runs the game-mode state machine, loads levels, handles checkpoints, deaths and the boss outcome, and draws all UI with IMGUI (title/level menu, HUD, touch buttons, results screens) |
| `Levels.cs` | Level data: name, allowed abilities, sky gradient and a `build` delegate that describes the geometry |
| `LevelBuilder.cs` | A small DSL for building geometry: `Plat`, `Rail`, `Coaster`, `Tube`, `Ice`, `Gate`, `Pad`, `Spinner`, `Mover`, `Glass`, `Box`, `Seesaw`, `Turret`, `Orb(s)`, `Check`, `Goal`, `Label`, `Rival`. It also tracks the level bounds |
| `Ball.cs` | The player: physics, abilities, the state/sprite machine, damage, triggers and the rail/tube/ice behaviour |
| `Boss.cs` | The red rival ball for level 5: AI state machine, ram resolution and lives |
| `Tile.cs` | Tags any collider with a `TileKind` (Solid, Rail, Tube, Ice, Gate, Orb, Check, Goal, Pad, Glass, …) and runs the per-tile animation (gate pulsing, orb bobbing) |
| `Controls.cs` | Turns keyboard, mouse and multi-touch into named actions (`Held/Pressed/Released("jump")`, `Move`) and lays out the touch buttons |
| `CameraFollow.cs` | Smooth follow with velocity look-ahead and speed-based zoom, clamped to the level bounds. Has a fixed-arena mode for the boss, plus the parallax sky and clouds |
| `Fx.cs` | Pooled particles, rings, screen shake and hit-stop (a brief `Time.timeScale` dip) |
| `Gfx.cs` | Colour palette, runtime-generated sprites (circle, glow, ring, triangle, 9-sliced rounded rectangle, vertical gradients) and loading of the ball-state sprites |
| `Sfx.cs` | Synthesised one-shot sound effects |
| `Music.cs` | Synthesised, looping background music for each level, plus the mute setting |
| `Bullet.cs`, `Turret.cs`, `Mover.cs`, `Spinner.cs`, `PadPower.cs` | Small behaviours for obstacles |
| `Editor/BuildWebGL.cs` | Headless build entry point: generates the scene, applies player settings and builds WebGL |
| `Editor/SpriteImport.cs` | Asset postprocessor that forces the correct sprite import settings on `Resources/Sprites` (256 px per unit, bilinear, uncompressed) |

---

## 2. Frameworks, APIs and tools used

### Engine and runtime
- **Unity 2022.3.45f1 LTS**, scripted in **C#**. For WebGL, Unity compiles the code with **IL2CPP** to C++ and then to **WebAssembly** (via Emscripten, bundled with Unity).
- `Packages/manifest.json` contains **only built-in modules**: `audio`, `imgui`, `jsonserialize`, `physics2d`, `ui`, `uielements`. There are no Asset Store or third-party packages.

### Unity APIs (by subsystem)
| Subsystem | APIs |
|---|---|
| Physics 2D (Box2D) | `PlatformEffector2D` (one-way rails), `Rigidbody2D` (dynamic, continuous collision detection, interpolation), `CircleCollider2D`, `BoxCollider2D`, `EdgeCollider2D` (rails, coasters, tube walls, ice), `PolygonCollider2D`, `HingeJoint2D` (seesaw), `PhysicsMaterial2D` (friction and bounce), `Physics2D.OverlapCircle` / `OverlapCircleNonAlloc` with `ContactFilter2D` (ground checks, grow/climb space checks, bullets), `OnCollisionEnter2D/Stay2D`, `OnTriggerEnter2D/Stay2D`, `ContactPoint2D.normal` (surface tangents) |
| Rendering | `SpriteRenderer`, `Sprite.Create` with 9-slice borders, `Texture2D.SetPixels`, the `Sprites/Default` shader, `sortingOrder` layering, and an orthographic `Camera` |
| UI | IMGUI: `OnGUI`, `GUI.DrawTexture`, `GUIStyle`, and resolution-independent layout (a unit of `Screen.height / 720`) |
| Input | `Input.GetKey*`, `Input.touches` (multi-touch), `Input.mousePosition` / `GetMouseButtonDown`. Touches are also delivered as mouse clicks, so menu buttons work with both |
| Audio | `AudioSource.PlayOneShot` (SFX), a looping `AudioSource` (music), and `AudioClip.Create` + `SetData` to build clips from sample buffers generated at runtime |
| Time and FX | `Time.timeScale` (hit-stop), `Time.unscaledDeltaTime` (UI and camera keep moving during hit-stop), coroutines (`IEnumerator`, `WaitForSeconds`) for respawn, level end and boss outcomes |
| Persistence | `PlayerPrefs` (music on/off survives reloads, stored in IndexedDB on WebGL) |
| Editor | `BuildPipeline.BuildPlayer`, `EditorSceneManager`, `PlayerSettings.WebGL.*` (gzip plus decompression fallback, custom template), `AssetPostprocessor`, `[MenuItem]` |

### Web
- **Custom WebGL template** `Assets/WebGLTemplates/Sky/index.html`: full-viewport canvas, a sky-gradient loader, `viewport-fit=cover`, `apple-mobile-web-app-capable`, and touch-action disabled so Safari doesn't scroll or zoom.
- Build output is **gzip-compressed** with a **decompression fallback**, so it runs on any static host (itch.io, GitHub Pages, `python -m http.server`) without special `Content-Encoding` headers.

### Tooling
| Tool | Used for |
|---|---|
| Unity Hub and editor (batch mode) | Licensing, and headless command-line builds (`-batchmode -nographics -executeMethod BuildWebGL.Build`) |
| Python 3, **Pillow**, **NumPy** | Cutting the ball states out of the concept art (seeded centres and radii, edge-contrast circle fitting, circular alpha masks, normalised 512 px output). Also generating the red rival sprites by rotating the HSV hue of the blue sprites |
| Playwright (Chromium over CDP) | Automated browser play-tests of the WebGL build: scripted key presses and screenshots of each level, the boss fight and the menu |
| **butler** (itch.io CLI) | Uploading `webgl/` to the `slavastar/game:html` channel |
| GitHub Actions + `manleydev/butler-publish-itchio-action` | Automatic deploys to itch.io on pushes to `main` |

---

## 3. Physics model (the "feel")

The ball is a single dynamic `Rigidbody2D` with a `CircleCollider2D`. Movement uses forces rather than setting the velocity directly, so slopes, rails and collisions stay physically consistent.

| Parameter | Value |
|---|---|
| World gravity | `-9.81`, ball `gravityScale = 3` (a snappier arcade arc) |
| Radius | 0.50 normal · 0.32 crouched · 0.85 grown |
| Max rolling speed | 10 (7 crouched, 7.5 grown, 16 on rails) |
| Coyote time / jump buffer | 0.10 s / 0.12 s |
| Dash cooldown | 0.9 s (gravity switched off for the length of the dash) |
| Hearts | 3, with 1.1 s of invulnerability after a hit |

Game-feel layers on top of the simulation:
- **Squash and stretch** based on impact speed and direction. The sprite spins according to the angular velocity.
- **Hit-stop** (about 0.05–0.1 s of `timeScale` dip), **screen shake** and pooled **particle bursts** on landings, dashes, hits and pickups.
- **Rails and tubes:** in `OnCollisionStay2D` the ball reads the contact normal, works out the surface tangent and adds force along it in the direction it's already moving (+9 on rails, +13 in tubes), so it keeps its speed through dips and loops. One-way rails use `PlatformEffector2D`, so the ball can jump up through them from below.
- **Ice:** a `TileKind.Ice` contact switches the ball to the FREEZE sprite, turns off steering and jumping, and pushes the ball along the direction of the ice until it leaves.
- **Reverse gravity:** flips `gravityScale` and the "up" vector used by every ability. It only recharges when the ball touches a surface, which stops mid-air spamming.
- **Grow:** before growing, the ball checks for free space with an overlap query. Mass rises so the ball can shove crates and sink seesaws.
- **Climb:** hold to stick to walls, limited by a stamina meter (3 s).

### Lasers (red and blue)
Every gate is a trigger collider whose `Tile` runs a clock: `GateLive = repeat(time + shift, on + off) < on`, with on = 1.4 s and off = 1.3 s. A `shift` offsets neighbouring gates so they don't all fire together. The beam flickers just before it switches on.
- **Red gate** (`push = false`): while live, it calls `Ball.Hurt()`, which costs one heart and knocks the ball back.
- **Blue gate** (`push = true`): while live, it calls `Ball.Push()`, which only knocks the ball away from the beam (velocity of 9 sideways plus 5 up, 0.35 s re-trigger guard). Hearts are never touched.

---

## 4. Levels

Each level is one `Levels.Level` entry:

```csharp
new Level { name = "PULSE", unlock = "jump,dash", build = PhaseShift,
            top = C(246,134,128), mid = C(255,196,170), bottom = C(255,236,204) }
```

- `unlock` sets which abilities, and therefore which on-screen buttons, the level allows. Each level uses 2–3, which keeps the controls readable on a phone.
- `build` is a static method that calls the `LevelBuilder` DSL, for example:

```csharp
b.Plat(153.75f, 4, 7.5f);                 // floating platform (x, y, width[, height])
b.Gate(155, 4.35f, 7.5f, 0, true);        // blue push laser
b.Tube(V(157.4f,5.13f), V(160,5.1f), V(163,2.7f), V(166,3.3f), V(168.6f,4.63f));
b.Ice(175.3f, 3.5f, 180, 2, 1);           // ice slope, slides right
b.Check(146, 5.4f);                       // checkpoint
b.Goal(206, 4.5f);                        // level exit
```

`LevelBuilder` stretches the level bounds to fit every piece it creates. The camera clamps to those bounds, and a ball that falls below them loses a life and respawns at the last checkpoint.

| # | Level | Abilities | Theme and main mechanics | Length (world units) |
|---|---|---|---|---|
| 1 | FIRST FLIGHT | jump, crouch, dash | morning blue: glass, squeeze block, S-tube, roller-coaster, ice slope, blue lasers | about 210 |
| 2 | UPSIDE | jump, reverse, climb | lilac dusk: gravity flips, ceiling runs, wall climbs, rail-to-rail jumps | about 181 |
| 3 | HEAVY WEATHER | jump, grow | icy mint: crates, seesaws, ice slide, U-tube, coaster | about 197 |
| 4 | PULSE | jump, dash | sunset peach: pulsing red and blue lasers, S-tubes, rails | about 150 |
| 5 | DUEL | jump, dash | dusk arena: boss fight on one fixed screen | 22 × 11 arena |

Levels 1–4 are roughly 30–45% longer than in the previous version, and each has a checkpoint where its new section starts.

---

## 5. Boss: the red rival (`Boss.cs`)

**Arena.** The arena is a closed room of 22 × 11 units: floor, two walls, a ceiling and three floating platforms. `CameraFollow.fixedView` locks the camera to the centre of the arena and sizes it to fit the aspect ratio, so the whole fight stays on one screen.

**Lives.** Both balls have 3 hearts. The rival's hearts are shown in a RED pill at the top centre of the screen.

**Ram resolution** (`Boss.OnCollisionEnter2D`). Using each ball's velocity from before the collision (`LastVel`):

```
toPlayer  = normalize(player.pos - boss.pos)
bossHit   = dot(boss.LastVel,   toPlayer) * boss.mass
playerHit = dot(player.LastVel, -toPlayer) * player.mass
if playerHit > 5 and playerHit > bossHit + 1: boss loses a life
elif bossHit > 5 and bossHit > playerHit + 1: player loses a life
else: both bounce apart, nobody is hurt
```

Whoever is moving into the other ball harder wins the exchange. A dash (speed of about 20) is the player's strongest attack, and rolling into each other at the same speed is a draw.

**AI.** The rival runs a telegraphed state machine. It rolls toward the player, at most 7 units/s against the player's 10, jumps when the player is above it, and backs off when the player dashes at it. Every 1.8–2.8 s it telegraphs an attack: its glow pulses, a ring is emitted and its sprite changes form. Then it picks one of:

| Move | Telegraph | Effect |
|---|---|---|
| Shoot | "parry" form, 0.4 s | 2 bullets in a spread (3 on its last life), speed 7 |
| Dash | "spin" form, 0.55 s | 0.28 s charge at the player, speed 14, no gravity |
| Grow | "grow" form | 3 s with a larger radius and 3× mass: slower, but its rams hit harder |
| Blink | "teleport" form | Teleports to the far side of the arena, then shoots |

**Difficulty tuning (medium).** The rival is slower than the player, every attack is telegraphed, it has 1.4 s of invulnerability after a hit and pauses 1.2 s to think afterwards, and bullets are slow and can be dodged. It gets more aggressive as it loses lives: its think time shrinks and it fires 3 shots on its last life. If the player loses all 3 hearts, "RED WINS" is shown and the duel restarts. Winning shows CHAMPION and then the results screen.

---

## 6. Audio: synthesised at runtime

The game ships **no audio files**. Every sound is computed into a `float[]` buffer and wrapped with `AudioClip.Create`.

- **SFX (`Sfx.cs`).** Short square, sine and noise sweeps with exponential envelopes (jump, bounce, land, dash, hurt, pickup, …). Each play varies the pitch randomly by about ±6% so repeats don't sound identical.
- **Music (`Music.cs`).** One calm loop per level, rendered at 22.05 kHz and mono the first time the level is played, then cached:
  - **Pad:** each chord tone is a sine plus a detuned sine (a 0.4% chorus) plus a soft octave, with a 1.1 s smoothstep attack and release. Chords overlap into the next one, and writes wrap modulo the buffer length, so the loop is **seamless**.
  - **Bass:** the chord root an octave lower.
  - **Bells:** a sine plus a quiet 3× partial with an exponential decay, playing a gentle arpeggio pattern. The boss track uses eighth notes for a little more tension.
  - **Moods:** L1 C–Am–F–G (bright morning) · L2 Dmaj7–Bm7–Gmaj7–A (dreamy lilac) · L3 Em–C–G–D with bells two octaves up (icy) · L4 F–Dm–B♭–C (warm sunset) · L5 Am–F–Dm–E (calm but tense duel).
  - The mix is normalised to a peak of 0.8 and played at a volume of 0.32, fading in and out at 0.25/s.
- **Mute.** The MUSIC/MUTED pill (top-right) or the `M` key toggles music, and the setting is saved in `PlayerPrefs`. Browsers only allow audio after the first user gesture, and Unity resumes the audio context on the first click or tap.

---

## 7. UI and mobile

- **IMGUI HUD.** Level number, hearts, orb count, level name, timer and falls, plus MENU and MUSIC pills. The boss level adds the rival's hearts.
- **Level menu (title screen).** Five cards, one per level, each showing its sky colour, number, name and abilities. You can click or tap a card, press `1`–`5`, or use ←/→ and `Enter`.
- **Touch** (`Controls.Touch` switches on at the first touch):
  - Fixed ◀ ▶ pads in the lower left. Their hit zones cover the whole lower-left area, and you can slide between them without lifting your thumb.
  - A large JUMP button at the bottom right, with only the current level's abilities arranged in an arc around it. Each ability button shows its cooldown and greys out while unavailable.
  - Holding the phone in portrait shows a "rotate your phone" screen.
- Layout scales with a unit of `Screen.height / 720`, so it matches from a phone up to a 4K monitor.

---

## 8. Build and deployment pipeline

```
C# sources ──► Unity batch build (BuildWebGL.Build)
                 • regenerates Main.unity (camera only)
                 • PlayerSettings: 1280×720, gzip + fallback, template "Sky",
                   minimal managed stripping
                 • BuildPipeline.BuildPlayer → ../webgl
            ──► webgl/ committed to the repo
            ──► butler push webgl slavastar/game:html   (manual)
                or GitHub Actions on main                (automatic)
            ──► https://slavastar.itch.io/game
```

`BuildWebGL.Build` prints `BUILD RESULT: Succeeded size=<bytes>` (currently about 12.6 MB), so CI or scripts can grep the log to check the result.

---

## 9. Testing approach

- **Compilation:** every change is checked with a full headless Unity WebGL build, which fails on any C# error.
- **Automated play-tests:** Playwright drives Chromium against the build served over HTTP. For each level it jumps to checkpoints with a debug key (`F9`), holds inputs, presses jumps and ability keys, and screenshots every ~300 ms. The screenshots are assembled into contact sheets to confirm that sections can be crossed and the goal reached. The boss fight is scripted the same way to confirm that the rival moves, shoots, changes form, loses lives and that both outcomes (RED WINS and CHAMPION) happen.
- **Mobile:** iPhone-sized landscape viewports with touch emulation check the touch layout.
- **Debug keys:** `N` skips to the next level, `F9` warps to the next checkpoint and `R` restarts the level.

---

## 10. Known limitations and future work

- The HUD uses IMGUI, which is simple and fast to iterate on but not accessibility-aware. Moving to UI Toolkit would add proper focus navigation.
- The boss AI is a hand-tuned state machine. It has no difficulty setting yet.
- A native iOS build is possible (Unity iOS target plus Xcode), but the web build was chosen so the game plays instantly from a link.
