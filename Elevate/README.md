# Elevate

A minimalist top-down grid puzzle made with Unity 6 (6000.0.84f1). Move your piece to the green
square without falling into holes. Some tiles are raised platforms: climb one level at a time,
drop down any height, and jump two tiles from a raised platform.

## Tiles

| Tile | Meaning |
| --- | --- |
| Dark tile | Floor |
| Tall slate block | Wall |
| Red glowing hole | Bounces you back to where you were |
| Green | Goal (every piece must stand on one) |
| Indigo with dots | Platform, dots = height (1-3) |
| Purple | Lift, flips between low and high when you step on the purple switch |
| Cyan | Pulse platform, height cycles 0-1-2-1 with every move |
| Pale blue | Ice, you slide until something stops you |
| Pink ring | Portal to its twin |
| Orange chevron | Arrow that pushes you one tile |

## Controls

- Move: WASD / arrow keys, or tap a highlighted dot
- Jump (2 tiles, only from a raised platform): hold Shift + direction, press Space then a direction, or tap a ring
- Undo: Z, Restart: R, Level select: Esc

## Project layout

- `Assets/Scripts` – all game code. The game builds itself at runtime (`GameManager.Boot`), so the scene is empty.
- `Assets/Resources/levels.txt` – level definitions (legend at the top of the file).
- `Tools/solve.py` – BFS solver that mirrors `Board.cs`; run `python3 Tools/solve.py` after editing levels to check each level is solvable and see the optimal move count (use it as `par`).

## Build

Open the folder in Unity 6000.0.84f1 and use **Elevate > Build WebGL**, or from the command line:

```bash
Unity -batchmode -nographics -projectPath Elevate -executeMethod BuildScript.BuildWebGL -logFile -
```

The build is written to `Elevate/Build/WebGL`.
