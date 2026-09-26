# SKYROLL

A minimal physics game in **Unity 2022.3 (WebGL)**. You roll a glossy ball across floating platforms in the sky and change its state (dash, grow, reverse gravity, climb) to get past rails, glass tubes, ice and pulsing lasers. The last level is a duel against a red rival ball.

**Play:** https://slavastar.itch.io/game (desktop, or iPhone in Safari held sideways)

| | |
|---|---|
| ![Level 1](docs/screenshots/level1.png) | ![Level 4](docs/screenshots/level4.png) |
| ![Boss duel](docs/screenshots/boss.png) | |

## Main concepts
- **Physics feel:** Unity 2D physics (Box2D) plus squash and stretch, hit-stop, screen shake and particles.
- **Ball states:** each ability swaps the ball's sprite. The sprites are cut from our own concept art.
- **5 levels:** you can pick any of them from the menu. Each level uses only 2–3 abilities.
- **Lasers:** red lasers cost a life, blue lasers only push you back.
- **Boss:** a single fixed screen with 4 lives each. Ram the red ball to take one of its lives. It fights back by shooting, dashing, growing and teleporting.
- **Music:** calm music for each level, synthesised in code. Press `M` to mute.
- **Mobile:** fixed ◀ ▶ pads, a JUMP button and buttons for the level's abilities. A rotate screen appears in portrait.

## Controls
`A/D` roll · `Space` jump · `S` crouch · `Shift` dash · `E` reverse gravity · `C` climb · `G` grow · `R` restart · `L` level menu · `M` music

## Run locally
```bash
cd webgl && python3 -m http.server 8091   # open http://localhost:8091
```

## Build and deploy
```bash
cd unity
Unity -batchmode -nographics -quit -projectPath . -buildTarget WebGL \
      -executeMethod BuildWebGL.Build -logFile build.log   # outputs ../webgl
butler push ../webgl slavastar/game:html                     # itch.io
```
Open `unity/` in Unity Hub (2022.3.45f1 with WebGL support). The whole game is generated from C# at runtime (see `unity/Assets/Scripts`).

## Stack
Unity 2022.3 · C# · Physics2D · IMGUI · procedural audio (`AudioClip.Create`) · Python/Pillow for sprites · butler and GitHub Actions for itch.io.

More detail: [docs/TECHNICAL.md](docs/TECHNICAL.md).
