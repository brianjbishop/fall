## Devlog — F'all · May 2026

b. was building a Unity arcade game for a 4-player circular cabinet — an installation where each player sits at a corner and dodges falling obstacles. The physical setup dictated everything: color-coded controls (Yellow, Green, Red, Blue) mapped to directions, a dual-sided game-over screen readable from opposite ends of the cabinet, and clouds that needed to persist visually across scene transitions so the environment never flickered. The whole game was designed and iterated in a single 16-day Claude Code session.

The first structural challenge was scene management. The original design was a single-scene game; the final version needed a real flow — StartScene → CountdownScene → any of eight level variants → GameOverScene — with score, hearts, and speed multiplier surviving every transition. Getting persistent state right was the load-bearing decision that everything else depended on: the `PersistObject` + `DontDestroyOnLoad` pattern provided a root persistent GameObject that GameManager, CloudSpawner, and the HUD canvas all lived on, surviving scene loads without re-initialization.

The spawner design went through the most iteration. The original `QuadrantSpawner` cycled through Q0→Q1→Q2→Q3 in a fixed order with timing baked in — fast to prototype, hard to design levels with. The rewrite introduced `SpawnPhase`: a serializable data structure defining which quadrants are active (via bitmask, so multiple can run simultaneously), how fast objects spawn, how aggressively that rate accelerates, and for how long. Phases execute sequentially via coroutines. A parallel tuning effort replaced raw decimal fields with Inspector sliders, mapping `spawnRate 1–10` to real intervals (3.0s→0.3s) and `accelerationRate 1–10` to per-second acceleration values — enough range for both easy warm-up phases and aggressive endgame patterns.

Several bugs only surfaced when the full system ran together. The music controller was restarting the soundtrack on every level load because `GameStarted` fires once per scene. The persistent object cleanup on restart was using scene-name lookup (`"DontDestroyOnLoad"`) which proved unreliable — objects accumulated across restarts. The GameOver scene was selecting itself as a random level because the `LevelSceneIndices` default array included the wrong index. Each fix followed the same logic: make the behavior explicit (an `isPlaying` guard, a registered object list, a correct index array) rather than inferring it from Unity internals.

---

### Multi-Scene Flow and Persistent State — replacing a single-scene design

**Why:** The original game was all in one scene. The cabinet needed a real arc: a startup moment, a countdown before each level, multiple level variants that can repeat, and a distinct game-over screen that's readable from across the cabinet.

**What:** `GameManager` was rewritten with `DontDestroyOnLoad` — it lives on a root `Persistent` GameObject alongside the HUD canvas and CloudSpawner. `PersistObject.cs` handles the registration: anything that needs to persist attaches this component and registers itself in `GameManager.registeredPersistentObjects`. `SceneIndex.cs` centralizes all scene build indices as constants. `ResetStats(fullReset)` distinguishes between level transitions (keep hearts and score) and full game restart (reset everything). `DestroyAllPersistent()` iterates the explicit registration list rather than searching by scene name — reliable across restarts.

**Impact:** Any script can access game state anywhere in the flow. Scene transitions are clean. Adding or removing level scenes only requires updating `SceneIndex.cs`.

---

### ObstacleSpawner — phase-based coroutine system replacing fixed quadrant cycling

**Why:** `QuadrantSpawner` (the original name) cycled Q0→Q1→Q2→Q3 with timing baked in. It wasn't possible to design "Q1 and Q2 together for 5 seconds, then just Q4 for 2 seconds" — the kind of variation needed to make levels feel distinct.

**What:** Renamed to `ObstacleSpawner.cs` (GUID preserved so scene references held). Takes a `SpawnPhase[]` array; phases run sequentially via coroutines. Each phase specifies active quadrants as a bitmask (so any combination can be active simultaneously), spawn rate, acceleration rate, and duration. `levelStartTime` tracks when the current level loaded — `startDelay` on each phase is relative to that, not to global playtime.

**Impact:** Level design becomes a data-editing task in the Inspector. Eight level variants (SpiralSlow, SpiralQuick, AllEasy, AllHard, LeftRight, Checker, and others) are each a distinct `SpawnPhase[]` configuration on the same spawner script.

---

### Inspector Sliders for Spawn Parameters — replacing raw decimals with a tunable range

**Why:** Fields like `spawnIntervalDecreasePerSecond = 0.15` were hard to reason about at design time. The designer needed to think in relative terms: "slow start, aggressive ramp" or "fast and constant" — not in seconds-per-second arithmetic.

**What:** `spawnRate` and `accelerationRate` replaced raw floats with `[Range(1, 10)] float` sliders. `spawnRate` maps 1→3.0s interval, 10→0.3s interval. `accelerationRate` maps 1→0.03/s, 10→0.30/s. `duration` gets `[Range(1, 30)]`, `startDelay` gets `[Range(0, 120)]`. A single `minSpawnInterval` on `ObstacleSpawner` (default 0.33s) floors the acceleration — spawn rate can't go infinite regardless of settings. All slider values are `float` for continuous fine-tuning between integers.

**Impact:** Level design iteration dropped from "edit, calculate, test" to "drag slider, test." Extreme settings are bounded by the min interval without requiring code changes.

---

### Difficulty Progression — speed multiplier that compounds across levels

**Why:** Every level played at the same pace. The physical cabinet was meant to be endurance-based — the game should get harder the longer you survive.

**What:** `GameManager.SpeedMultiplier` starts at 1.0 and increments by `speedIncreasePerLevel` (default 0.05, Inspector-tunable) on each level complete. `FlyingUpward.cs` and `FallingDown.cs` both apply the multiplier at movement time: `movementSpeed * GameManager.Instance.SpeedMultiplier`. Resets to 1.0 on full restart.

**Impact:** Each level completion makes the next 5% faster. The difficulty curve emerges from play progression rather than from hand-editing per-level parameters.

---

### Cloud Persistence — parenting spawned objects to a persistent root

**Why:** Clouds were spawned as scene objects and destroyed when any scene transition happened. The cabinet's visual environment needed to feel continuous — clouds drifting through level loads, not blinking out and back.

**What:** `OuterSpawner.cs` renamed to `CloudSpawner.cs`. The spawner itself lives on the `Persistent` root (DontDestroyOnLoad). Spawned clouds are set as children of CloudSpawner at instantiation, so they inherit its persistence. A scene guard prevents spawning during StartScene and CountdownScene — clouds only populate level scenes.

**Impact:** Visual continuity across all transitions. Clouds accumulate and drift naturally throughout a full game session.

---

### Audio Lifecycle — music that starts on intent, not on event

**Why:** `CameraAudioController` subscribed to `GameStarted` to start music. `GameStarted` fires once per scene load — including CountdownScene — so music restarted at the beginning of every level.

**What:** `OnGameStarted()` now checks `isPlaying` before calling `Play()`. A separate `PlayMusic()` method starts music immediately without the guard. `CameraIntroDrop` calls `PlayMusic()` when the player presses any key in StartScene — music starts at the moment of intent, before the camera drops. Stops cleanly on `GameOverTriggered`.

**Impact:** Music plays continuously through the entire session (startup → all levels → game over), starting at the right dramatic moment and never resetting mid-game.

---

### Screen Edge Indicators — anchoring directional arrows to cabinet corners

**Why:** The directional highlight arrows (Yellow/Green/Red/Blue) weren't staying pinned to screen edges — they were snapping to a smaller bounding box that didn't match the actual display.

**What:** Two new components. `MatchCanvasSize.cs` stretches the parent RectTransform to exactly match its Canvas parent — solves the root sizing problem. `ScreenEdgeSnap.cs` takes an enum (Top/Bottom/Left/Right) and a configurable inset, and positions one indicator at that screen edge. Each arrow gets its own component instance pointing at its assigned edge.

**Impact:** Directional cues are reliable across any canvas resolution or cabinet screen size. The two-component pattern keeps responsibilities separate — sizing and positioning are independent concerns.
