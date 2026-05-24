# F'all

A Unity arcade game built for a 4-player circular cabinet. Players dodge falling obstacles,
each controlling a color-coded direction. The longer you survive, the faster things fall.

## What it is

F'all is an installation piece — a physical arcade cabinet where four players sit at the
corners and dodge objects dropping from above. Controls are color-coded (Yellow, Green, Red,
Blue) and mapped to directions based on where each player is seated. The game scales in
difficulty across levels, ends when hearts run out, and loops back to the start.

## Architecture

```
StartScene
    │ (any key press → camera drop → music starts)
    ▼
CountdownScene
    │ (3, 2, 1, GO)
    ▼
LevelScene (one of 8 variants)
    │ (ObstacleSpawner runs SpawnPhase[] sequence)
    │ (hearts hit 0 → GameOverScene)
    │ (level complete → next LevelScene, speed +5%)
    ▼
GameOverScene
    │ (dual-sided display for the circular cabinet)
    │ (any key → restart)
    ▼
StartScene
```

All game state (score, hearts, speed multiplier) lives on a persistent `GameManager`
that survives every scene transition via `DontDestroyOnLoad`.

## Key scripts

| Script | What it does |
|--------|-------------|
| `GameManager.cs` | Central state: score, hearts, speed multiplier, events |
| `ObstacleSpawner.cs` | Phase-based coroutine spawner with Inspector-tunable parameters |
| `SpawnPhase.cs` | Serializable data class defining one spawn phase |
| `PersistObject.cs` | Registers a GameObject for DontDestroyOnLoad + cleanup tracking |
| `PlayerMovingContr.cs` | Multi-key directional input, configurable per direction |
| `GameSettlementUI.cs` | HUD: hearts and score across levels |
| `GameOverController.cs` | Final screen with dual-sided score display |
| `CloudSpawner.cs` | Persistent cloud environment across scene loads |
| `CameraAudioController.cs` | Music lifecycle (starts on key press, stops on game over) |
| `ScreenEdgeSnap.cs` | Pins directional indicators to screen edges |

## Requirements

- Unity 6 (or compatible)
- TextMeshPro (included via Package Manager)

## Running

Open the project in Unity, add all scenes to Build Settings in order (StartScene first),
then hit Play from StartScene.

## Development notes

See [DEVELOPMENT.md](DEVELOPMENT.md) for the full story of how this was built — the
architectural decisions, the spawner design evolution, and the bugs that only appeared
when the full system ran together.
